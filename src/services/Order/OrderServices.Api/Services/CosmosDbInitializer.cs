using Microsoft.Azure.Cosmos;
using OrderServices.Api.Configuration;
using OrderServices.Infra.Models;

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

            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _configuration.DatabaseName,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Database '{DatabaseName}' ready", _configuration.DatabaseName);

            var containerProperties = new ContainerProperties
            {
                Id = _configuration.ContainerName,
                PartitionKeyPath = _configuration.PartitionKey,
                DefaultTimeToLive = -1 
            };
            containerProperties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
            containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            
            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400, 
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Container '{ContainerName}' ready with partition key '{PartitionKey}'",
                _configuration.ContainerName,
                _configuration.PartitionKey);

            // Seed data
            await SeedDataAsync(containerResponse.Container, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Cosmos DB");
            throw;
        }
    }

    private async Task SeedDataAsync(Container container, CancellationToken cancellationToken)
    {
        try
        {
            // Check for invalid data (old seed with invalid GUIDs) and clean if necessary
            var hasInvalidData = await HasInvalidSeedDataAsync(container, cancellationToken);
            
            if (hasInvalidData)
            {
                _logger.LogWarning("🔄 Found invalid seed data with incorrect GUID format. Cleaning up...");
                await CleanupInvalidDataAsync(container, cancellationToken);
                _logger.LogInformation("✅ Invalid data cleaned up successfully");
            }

            // Check if valid data already exists
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.type = 'Order'");
            var iterator = container.GetItemQueryIterator<int>(query);
            var response = await iterator.ReadNextAsync(cancellationToken);
            var count = response.FirstOrDefault();

            if (count > 0)
            {
                _logger.LogInformation("Seed data already exists ({Count} orders found), skipping...", count);
                return;
            }

            _logger.LogInformation("🌱 Seeding sample orders to Cosmos DB...");

            var sampleOrders = GenerateSampleOrders();

            foreach (var order in sampleOrders)
            {
                await container.CreateItemAsync(
                    order,
                    new PartitionKey(order.CustomerId),
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("✅ Successfully seeded {Count} sample orders", sampleOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error seeding data, continuing without seed data");
        }
    }

    /// <summary>
    /// Checks if the container has invalid seed data (ProductIds starting with 'p' instead of valid GUIDs)
    /// </summary>
    private async Task<bool> HasInvalidSeedDataAsync(Container container, CancellationToken cancellationToken)
    {
        try
        {
            // Query for orders with ProductId starting with 'p' (old invalid format)
            var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c JOIN oi IN c.orderItems WHERE STARTSWITH(oi.productId, 'p') AND c.type = 'Order'");
            
            var iterator = container.GetItemQueryIterator<int>(query);
            var response = await iterator.ReadNextAsync(cancellationToken);
            var invalidCount = response.FirstOrDefault();

            return invalidCount > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes all orders with invalid ProductId format
    /// </summary>
    private async Task CleanupInvalidDataAsync(Container container, CancellationToken cancellationToken)
    {
        try
        {
            // Get all orders
            var query = new QueryDefinition("SELECT c.id, c.customerId FROM c WHERE c.type = 'Order'");
            var iterator = container.GetItemQueryIterator<dynamic>(query);
            
            var itemsToDelete = new List<(string id, string partitionKey)>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    string id = item.id;
                    string customerId = item.customerId;
                    itemsToDelete.Add((id, customerId));
                }
            }

            _logger.LogInformation("🗑️ Deleting {Count} orders with invalid data...", itemsToDelete.Count);

            foreach (var (id, partitionKey) in itemsToDelete)
            {
                try
                {
                    await container.DeleteItemAsync<dynamic>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Item already deleted, continue
                }
            }

            _logger.LogInformation("✅ Cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
            throw;
        }
    }

    private List<OrderDocument> GenerateSampleOrders()
    {
        var customers = new[]
        {
            (Id: "c1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c", Name: "Ahmet Yılmaz"),
            (Id: "d2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d", Name: "Ayşe Demir"),
            (Id: "e3c4d5e6-f7a8-4b9c-0d1e-2f3a4b5c6d7e", Name: "Mehmet Kaya"),
            (Id: "f4d5e6f7-a8b9-4c0d-1e2f-3a4b5c6d7e8f", Name: "Fatma Şahin"),
            (Id: "a5e6f7a8-b9c0-4d1e-2f3a-4b5c6d7e8f9a", Name: "Ali Öztürk")
        };

        var products = new[]
        {
            (Id: "11111111-aaaa-bbbb-cccc-ddddeeee1111", Name: "iPhone 15 Pro", Price: 54999.99m),
            (Id: "22222222-aaaa-bbbb-cccc-ddddeeee2222", Name: "MacBook Air M3", Price: 47999.99m),
            (Id: "33333333-aaaa-bbbb-cccc-ddddeeee3333", Name: "AirPods Pro 2", Price: 7999.99m),
            (Id: "44444444-aaaa-bbbb-cccc-ddddeeee4444", Name: "Apple Watch Ultra 2", Price: 29999.99m),
            (Id: "55555555-aaaa-bbbb-cccc-ddddeeee5555", Name: "iPad Pro 12.9", Price: 42999.99m),
            (Id: "66666666-aaaa-bbbb-cccc-ddddeeee6666", Name: "Samsung Galaxy S24", Price: 44999.99m),
            (Id: "77777777-aaaa-bbbb-cccc-ddddeeee7777", Name: "Sony WH-1000XM5", Price: 11999.99m),
            (Id: "88888888-aaaa-bbbb-cccc-ddddeeee8888", Name: "Dell XPS 15", Price: 52999.99m)
        };

        var statuses = new[]
        {
            new OrderStatusDocument { Id = 1, Name = "Pending" },
            new OrderStatusDocument { Id = 2, Name = "Confirmed" },
            new OrderStatusDocument { Id = 3, Name = "Paid" },
            new OrderStatusDocument { Id = 4, Name = "Shipped" },
            new OrderStatusDocument { Id = 5, Name = "Delivered" }
        };

        var orders = new List<OrderDocument>();
        var random = new Random(42); // Fixed seed for reproducibility
        var orderId = 1;

        foreach (var customer in customers)
        {
            // Each customer gets 2-3 orders
            var orderCount = random.Next(2, 4);
            
            for (var i = 0; i < orderCount; i++)
            {
                var status = statuses[random.Next(statuses.Length)];
                var orderDate = DateTime.UtcNow.AddDays(-random.Next(1, 60));
                
                // Each order has 1-3 items
                var itemCount = random.Next(1, 4);
                var items = new List<OrderItemDocument>();
                var usedProducts = new HashSet<int>();
                
                for (var j = 0; j < itemCount; j++)
                {
                    int productIndex;
                    do
                    {
                        productIndex = random.Next(products.Length);
                    } while (usedProducts.Contains(productIndex));
                    
                    usedProducts.Add(productIndex);
                    var product = products[productIndex];
                    
                    items.Add(new OrderItemDocument
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = random.Next(1, 4),
                        UnitPrice = product.Price
                    });
                }

                var totalAmount = items.Sum(item => item.UnitPrice * item.Quantity);

                orders.Add(new OrderDocument
                {
                    Id = orderId.ToString(),
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    OrderDate = orderDate,
                    Status = status,
                    TotalAmount = totalAmount,
                    OrderItems = items,
                    Type = "Order"
                });

                orderId++;
            }
        }

        return orders;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cosmos DB initializer stopped");
        return Task.CompletedTask;
    }
}
