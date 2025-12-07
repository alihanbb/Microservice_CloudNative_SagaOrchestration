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
                clientOptions.ConsistencyLevel = ConsistencyLevel.Session;

                // Use Gateway mode for emulator (Direct mode requires specific ports)
                if (builder.Environment.IsDevelopment())
                {
                    clientOptions.ConnectionMode = ConnectionMode.Gateway;
                    clientOptions.HttpClientFactory = () =>
                    {
                        HttpMessageHandler httpMessageHandler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                        return new HttpClient(httpMessageHandler);
                    };
                }
                else
                {
                    clientOptions.ConnectionMode = ConnectionMode.Direct;
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
        // KeyVault is optional in Development environment
        var keyVaultConnectionString = builder.Configuration.GetConnectionString("keyvault");
        var keyVaultUri = builder.Configuration["Aspire:Azure:Security:KeyVault:VaultUri"];

        if (string.IsNullOrEmpty(keyVaultConnectionString) && string.IsNullOrEmpty(keyVaultUri))
        {
            if (builder.Environment.IsDevelopment())
            {
                // Skip KeyVault in development if not configured
                return builder;
            }
            
            throw new InvalidOperationException(
                "KeyVault configuration is required. Set 'ConnectionStrings:keyvault' or 'Aspire:Azure:Security:KeyVault:VaultUri'");
        }

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
