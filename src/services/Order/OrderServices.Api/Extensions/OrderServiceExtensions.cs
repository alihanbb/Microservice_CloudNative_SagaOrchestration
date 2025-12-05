namespace OrderServices.Api.Extensions;

/// <summary>
/// Master extension for configuring all Order Service dependencies
/// </summary>
public static class OrderServiceExtensions
{
    /// <summary>
    /// Adds all Order Service dependencies to the service collection
    /// </summary>
    public static WebApplicationBuilder AddOrderService(this WebApplicationBuilder builder)
    {
        // Azure Services (Cosmos DB, Key Vault)
        builder.AddAzureServices();

        // Application Services (Application, Infrastructure, Endpoints)
        builder.Services.AddApplicationServices(builder.Configuration);

        // Health Checks
        builder.Services.AddOrderServiceHealthChecks(builder.Configuration);
        builder.Services.AddOrderServiceHealthChecksUI();

        // Swagger/OpenAPI
        builder.Services.AddOrderServiceSwagger();

        // CORS
        builder.Services.AddOrderServiceCors();

        return builder;
    }

    /// <summary>
    /// Configures the Order Service middleware pipeline
    /// </summary>
    public static WebApplication UseOrderService(this WebApplication app)
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
