using System.Net;
using BackupServices.Configuration;
using BackupServices.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackupServices.Services;
public class OrderSyncService : ISyncService
{
    private readonly SourceOrderDbConfiguration _sourceConfig;
    private readonly BackupOrderDbConfiguration _backupConfig;
    private readonly CosmosClient _sourceClient;
    private readonly CosmosClient _backupClient;
    private readonly ILogger<OrderSyncService> _logger;

    public string ServiceName => "order";

    public OrderSyncService(
        IOptions<SourceOrderDbConfiguration> sourceConfig,
        IOptions<BackupOrderDbConfiguration> backupConfig,
        ILogger<OrderSyncService> logger)
    {
        _sourceConfig = sourceConfig.Value;
        _backupConfig = backupConfig.Value;
        _logger = logger;

        _sourceClient = CreateCosmosClient(_sourceConfig.ConnectionString);
        
        _backupClient = CreateCosmosClient(_backupConfig.ConnectionString);
    }

    private CosmosClient CreateCosmosClient(string connectionString)
    {
        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        if (connectionString.Contains("localhost") || connectionString.Contains("127.0.0.1"))
        {
            options.HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(handler);
            };
            options.ConnectionMode = ConnectionMode.Gateway;
        }

        return new CosmosClient(connectionString, options);
    }

    public async Task InitializeBackupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Order backup database in CosmosDB...");

        try
        {
            var databaseResponse = await _backupClient.CreateDatabaseIfNotExistsAsync(
                _backupConfig.DatabaseName,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Backup database created/verified: {DatabaseName}", _backupConfig.DatabaseName);

            var containerProperties = new ContainerProperties
            {
                Id = _backupConfig.ContainerName,
                PartitionKeyPath = "/customerId"
            };

            await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Backup container created/verified: {ContainerName}", _backupConfig.ContainerName);

            var historyContainerProperties = new ContainerProperties
            {
                Id = "sync-history",
                PartitionKeyPath = "/serviceName"
            };

            await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                historyContainerProperties,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Order backup database schema initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Order backup database");
            throw;
        }
    }

    public async Task<SyncResult> SyncAsync(bool forceFullSync = false, CancellationToken cancellationToken = default)
    {
        var syncId = Guid.NewGuid();
        var result = new SyncResult
        {
            SyncId = syncId,
            ServiceName = ServiceName,
            Status = SyncStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting Order incremental sync. SyncId: {SyncId}, ForceFullSync: {ForceFullSync}",
                syncId, forceFullSync);

            await InitializeBackupDatabaseAsync(cancellationToken);

            var sourceOrders = await GetSourceOrdersAsync(cancellationToken);
            _logger.LogInformation("Found {Count} orders in source database", sourceOrders.Count);

            var backupOrders = await GetBackupOrdersAsync(cancellationToken);
            var backupDict = backupOrders.ToDictionary(o => o.Id);

            var backupContainer = _backupClient.GetContainer(_backupConfig.DatabaseName, _backupConfig.ContainerName);

            foreach (var order in sourceOrders)
            {
                if (backupDict.TryGetValue(order.Id, out var existing))
                {
                    if (forceFullSync || 
                        order.LastModified > existing.LastModified ||
                        order.Status != existing.Status)
                    {
                        await UpsertOrderAsync(backupContainer, order, cancellationToken);
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                    backupDict.Remove(order.Id);
                }
                else
                {
                    await UpsertOrderAsync(backupContainer, order, cancellationToken);
                    result.InsertedCount++;
                }
            }

            foreach (var deleted in backupDict.Values)
            {
                await DeleteOrderAsync(backupContainer, deleted, cancellationToken);
                result.DeletedCount++;
            }

            await RecordSyncHistoryAsync(result, cancellationToken);

            result.Status = SyncStatus.Completed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Order sync completed. Inserted: {Inserted}, Updated: {Updated}, Deleted: {Deleted}, Skipped: {Skipped}",
                result.InsertedCount, result.UpdatedCount, result.DeletedCount, result.SkippedCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order sync failed. SyncId: {SyncId}", syncId);
            result.Status = SyncStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }
    }

    private async Task<List<OrderSyncEntity>> GetSourceOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = new List<OrderSyncEntity>();

        try
        {
            var container = _sourceClient.GetContainer(_sourceConfig.DatabaseName, _sourceConfig.ContainerName);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'Order'");

            using var iterator = container.GetItemQueryIterator<OrderSyncEntity>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                orders.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get source orders");
            throw;
        }

        return orders;
    }

    private async Task<List<OrderSyncEntity>> GetBackupOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = new List<OrderSyncEntity>();

        try
        {
            var container = _backupClient.GetContainer(_backupConfig.DatabaseName, _backupConfig.ContainerName);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'Order'");

            using var iterator = container.GetItemQueryIterator<OrderSyncEntity>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                orders.AddRange(response);
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Backup container not found yet, will be created");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read backup orders");
        }

        return orders;
    }

    private async Task UpsertOrderAsync(Container container, OrderSyncEntity order, CancellationToken cancellationToken)
    {
        order.LastModified = DateTime.UtcNow;
        
        await container.UpsertItemAsync(
            order,
            new PartitionKey(order.CustomerId),
            cancellationToken: cancellationToken);
    }

    private async Task DeleteOrderAsync(Container container, OrderSyncEntity order, CancellationToken cancellationToken)
    {
        try
        {
            await container.DeleteItemAsync<OrderSyncEntity>(
                order.Id,
                new PartitionKey(order.CustomerId),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    private async Task RecordSyncHistoryAsync(SyncResult result, CancellationToken cancellationToken)
    {
        try
        {
            var historyContainer = _backupClient.GetContainer(_backupConfig.DatabaseName, "sync-history");

            var historyRecord = new
            {
                id = result.SyncId.ToString(),
                serviceName = ServiceName,
                syncedAt = DateTime.UtcNow,
                insertedCount = result.InsertedCount,
                updatedCount = result.UpdatedCount,
                deletedCount = result.DeletedCount,
                skippedCount = result.SkippedCount,
                success = result.Status == SyncStatus.Completed,
                errorMessage = result.ErrorMessage
            };

            await historyContainer.CreateItemAsync(
                historyRecord,
                new PartitionKey(ServiceName),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record sync history");
        }
    }
}
