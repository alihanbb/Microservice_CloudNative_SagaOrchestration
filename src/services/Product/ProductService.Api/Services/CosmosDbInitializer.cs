using MongoDB.Driver;
using ProductService.Api.Configuration;

namespace ProductService.Api.Services;

public class MongoDbInitializer : IHostedService
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoDbConfiguration _configuration;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(
        IMongoClient mongoClient,
        IConfiguration configuration,
        ILogger<MongoDbInitializer> logger)
    {
        _mongoClient = mongoClient;
        _configuration = configuration.GetSection(MongoDbConfiguration.SectionName)
            .Get<MongoDbConfiguration>() ?? new MongoDbConfiguration();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("?? Initializing MongoDB database and collection...");

            // Get database
            var database = _mongoClient.GetDatabase(_configuration.DatabaseName);

            _logger.LogInformation("? Database '{DatabaseName}' ready", _configuration.DatabaseName);

            // Check if collection exists
            var collectionNames = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            var collections = await collectionNames.ToListAsync(cancellationToken: cancellationToken);

            if (!collections.Contains(_configuration.CollectionName))
            {
                // Create collection
                await database.CreateCollectionAsync(_configuration.CollectionName, cancellationToken: cancellationToken);
                _logger.LogInformation("? Collection '{CollectionName}' created", _configuration.CollectionName);

                // Create indexes for better query performance
                var collection = database.GetCollection<MongoDB.Bson.BsonDocument>(_configuration.CollectionName);
                
                var indexKeys = Builders<MongoDB.Bson.BsonDocument>.IndexKeys
                    .Ascending("name")
                    .Ascending("categoryId");
                
                var indexOptions = new CreateIndexOptions { Name = "name_category_idx" };
                var indexModel = new CreateIndexModel<MongoDB.Bson.BsonDocument>(indexKeys, indexOptions);
                
                await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
                _logger.LogInformation("? Index 'name_category_idx' created on collection '{CollectionName}'", _configuration.CollectionName);
            }
            else
            {
                _logger.LogInformation("? Collection '{CollectionName}' already exists", _configuration.CollectionName);
            }

            _logger.LogInformation("?? MongoDB initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error initializing MongoDB");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? MongoDB initializer stopped");
        return Task.CompletedTask;
    }
}

