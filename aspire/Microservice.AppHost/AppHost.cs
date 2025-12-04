var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVault("keyvault");

var cosmosDb = builder.AddAzureCosmosDB("cosmos")
    .AddDatabase("orderdb");

var cosmosDbUI = builder.AddContainer("cosmos-ui", "publiccr.azurecr.io/cosmosdb/emulator/linux/azure-cosmos-emulator", "latest")
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "data-explorer")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "true")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1");

var orderService = builder.AddProject<Projects.OrderServices_Api>("orderservice")
    .WithReference(cosmosDb)
    .WithReference(keyVault)
    .WithEnvironment("CosmosDb__DatabaseName", "orderdb")
    .WithEnvironment("CosmosDb__ContainerName", "orders")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints();

builder.Build().Run();


