using Microsoft.Azure.Cosmos;
using OrderServices.Api.Configuration;

namespace OrderServices.Api.Services;

public class CosmosDbInitializer : IHostedService
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbConfiguration _configuration;
    private readonly ILogger<CosmosDbInitializer> _logger;

    public CosmosDbInitializer(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILogger<CosmosDbInitializer> logger)
    {
        _cosmosClient = cosmosClient;
        _configuration = configuration.GetSection(CosmosDbConfiguration.SectionName)
            .Get<CosmosDbConfiguration>() ?? new CosmosDbConfiguration();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing Cosmos DB database and container...");

            // Create database if not exists
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _configuration.DatabaseName,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Database '{DatabaseName}' ready", _configuration.DatabaseName);

            // Create container if not exists
            var containerProperties = new ContainerProperties
            {
                Id = _configuration.ContainerName,
                PartitionKeyPath = _configuration.PartitionKey,
                DefaultTimeToLive = -1 // Enable TTL but don't set default
            };

            // Configure indexing policy for better query performance
            containerProperties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
            containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            
            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400, // RU/s - can be adjusted
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Container '{ContainerName}' ready with partition key '{PartitionKey}'",
                _configuration.ContainerName,
                _configuration.PartitionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Cosmos DB");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cosmos DB initializer stopped");
        return Task.CompletedTask;
    }
}
