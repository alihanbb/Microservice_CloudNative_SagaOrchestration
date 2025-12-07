using Microsoft.Azure.Cosmos;
using OrderServices.Api.Configuration;
using OrderServices.Domain.Aggregate;
using OrderServices.Infra;

namespace OrderServices.Api.Extensions;

/// <summary>
/// Master extension for configuring all Order Service dependencies
/// </summary>
public static class OrderServiceExtensions
{
    public static WebApplicationBuilder AddOrderService(this WebApplicationBuilder builder)
    {
        // Azure Services (Cosmos DB, Key Vault)
        builder.AddAzureServices();

        // Application and Infrastructure Services (with CosmosDB)
        var cosmosClient = builder.Services.BuildServiceProvider().GetRequiredService<CosmosClient>();
        builder.Services.AddApplicationServices(builder.Configuration, cosmosClient);

        // Health Checks
        builder.Services.AddOrderServiceHealthChecks(builder.Configuration);
        builder.Services.AddOrderServiceHealthChecksUI();

        // Swagger
        builder.Services.AddOrderServiceSwagger();

        // CORS
        builder.Services.AddOrderServiceCors();

        return builder;
    }

    public static async Task<WebApplication> UseOrderServiceAsync(this WebApplication app)
    {
        // Exception handling (first in pipeline)
        app.UseApplicationMiddleware();

        // Swagger (Development only)
        app.UseOrderServiceSwagger();

        // Health checks
        app.UseOrderServiceHealthChecks();

        // CORS
        app.UseCors();

        // HTTPS redirection
        app.UseHttpsRedirection();

        // Map endpoints
        app.MapApplicationEndpoints();

        // Startup logging
        app.LogStartupInfo();

        return app;
    }
}
