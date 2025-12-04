var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVault("keyvault");

var cosmosDb = builder.AddAzureCosmosDB("cosmos")
    .AddDatabase("orderdb");

var cosmosDbUI = builder.AddContainer("cosmos-ui", "publiccr.azurecr.io/cosmosdb/emulator/linux/azure-cosmos-emulator", "latest")
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "data-explorer")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "true")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1");

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sqlserver", password: sqlPassword, port: 1433)
    .WithDataVolume("customerdb-data")
    .WithLifetime(ContainerLifetime.Persistent);

var customerDb = sqlServer.AddDatabase("customerdb");

var adminer = builder.AddContainer("adminer", "adminer", "latest")
    .WithHttpEndpoint(port: 8082, targetPort: 8080, name: "adminer-ui")
    .WithEnvironment("ADMINER_DEFAULT_SERVER", "sqlserver");

var mongoPassword = builder.AddParameter("mongo-password", secret: true);

var mongodb = builder.AddMongoDB("mongodb", password: mongoPassword, port: 27017)
    .WithDataVolume("mongodb-data")
    .WithLifetime(ContainerLifetime.Persistent);

var productDb = mongodb.AddDatabase("productdb");

var mongoExpress = builder.AddContainer("mongo-express", "mongo-express", "latest")
    .WithHttpEndpoint(port: 8084, targetPort: 8081, name: "mongo-express-ui")
    .WithEnvironment("ME_CONFIG_MONGODB_SERVER", "mongodb")
    .WithEnvironment("ME_CONFIG_MONGODB_ADMINUSERNAME", "admin")
    .WithEnvironment("ME_CONFIG_MONGODB_ADMINPASSWORD", mongoPassword)
    .WithEnvironment("ME_CONFIG_BASICAUTH", "false");

var orderService = builder.AddProject<Projects.OrderServices_Api>("orderservice")
    .WithReference(cosmosDb)
    .WithReference(keyVault)
    .WithEnvironment("CosmosDb__DatabaseName", "orderdb")
    .WithEnvironment("CosmosDb__ContainerName", "orders")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: 5001, name: "orderservice-http")
    .WithExternalHttpEndpoints();

var customerService = builder.AddProject<Projects.CustomerServices_Api>("customerservice")
    .WithReference(customerDb)
    .WithReference(keyVault)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: 5003, name: "customerservice-http")
    .WithExternalHttpEndpoints();

var productService = builder.AddProject<Projects.ProductService_Api>("productservice")
    .WithReference(productDb)
    .WithReference(keyVault)
    .WithEnvironment("MongoDB__DatabaseName", "productdb")
    .WithEnvironment("MongoDB__CollectionName", "products")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: 5005, name: "productservice-http")
    .WithExternalHttpEndpoints();

var gateway = builder.AddProject<Projects.YarpGateway>("gateway")
    .WithReference(orderService)
    .WithReference(customerService)
    .WithReference(productService)
    .WithReference(keyVault)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: 5000, name: "gateway-http")
    .WithExternalHttpEndpoints();

builder.Build().Run();




