using Microsoft.Azure.Cosmos;
using OrderServices.Api.Configuration;
using OrderServices.Api.Services;

namespace OrderServices.Api.Extensions;

/// <summary>
/// Extension methods for Azure services configuration
/// </summary>
public static class AzureServiceExtensions
{
    /// <summary>
    /// Adds Azure Cosmos DB client with proper configuration
    /// </summary>
    public static WebApplicationBuilder AddAzureCosmosDb(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<CosmosDbConfiguration>(
            builder.Configuration.GetSection(CosmosDbConfiguration.SectionName));

        builder.AddAzureCosmosClient("cosmos",
            configureClientOptions: clientOptions =>
            {
                clientOptions.SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                };
                clientOptions.ConnectionMode = ConnectionMode.Direct;
                clientOptions.ConsistencyLevel = ConsistencyLevel.Session;

                if (builder.Environment.IsDevelopment())
                {
                    clientOptions.HttpClientFactory = () =>
                    {
                        HttpMessageHandler httpMessageHandler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                        return new HttpClient(httpMessageHandler);
                    };
                }
            });

        builder.Services.AddHostedService<CosmosDbInitializer>();

        return builder;
    }

    /// <summary>
    /// Adds Azure Key Vault client with proper configuration
    /// </summary>
    public static WebApplicationBuilder AddAzureKeyVault(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<KeyVaultConfiguration>(
            builder.Configuration.GetSection(KeyVaultConfiguration.SectionName));

        builder.AddAzureKeyVaultClient("keyvault");

        return builder;
    }

    /// <summary>
    /// Adds all Azure services (Cosmos DB, Key Vault)
    /// </summary>
    public static WebApplicationBuilder AddAzureServices(this WebApplicationBuilder builder)
    {
        builder.AddAzureCosmosDb();
        builder.AddAzureKeyVault();

        return builder;
    }
}
