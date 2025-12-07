using Microsoft.Azure.Cosmos;
using OrderServices.Api.Configuration;
using OrderServices.Api.Middleware;
using OrderServices.Application;
using OrderServices.Domain.Aggregate;
using OrderServices.Infra;

namespace OrderServices.Api.Extensions;

/// <summary>
/// Extension methods for application layer services
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds all application services (Application layer, Infrastructure layer, Endpoints)
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        CosmosClient cosmosClient)
    {
        // Application Layer (MediatR, FluentValidation, Behaviors)
        services.AddApplication();

        // Infrastructure Layer (CosmosDB Repository)
        var cosmosConfig = configuration.GetSection(CosmosDbConfiguration.SectionName)
            .Get<CosmosDbConfiguration>() ?? new CosmosDbConfiguration();
        
        var container = cosmosClient.GetContainer(cosmosConfig.DatabaseName, cosmosConfig.ContainerName);
        services.AddScoped<IOrderRepository>(_ => new CosmosOrderRepository(container));

        // Minimal API Endpoints
        services.AddEndpoints();

        return services;
    }

    /// <summary>
    /// Uses application middleware (Exception handling, etc.)
    /// </summary>
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Global exception handling - should be first in pipeline
        app.UseGlobalExceptionHandler();

        return app;
    }

    /// <summary>
    /// Maps all application endpoints and root info endpoint
    /// </summary>
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        // Map all minimal API endpoints
        app.MapEndpoints();

        // Root endpoint with service info
        app.MapGet("/", () => Results.Ok(new
        {
            Service = "Order Service API",
            Version = "v1",
            Status = "Running",
            Database = "CosmosDB",
            Timestamp = DateTime.UtcNow,
            Endpoints = new
            {
                Documentation = "/swagger",
                HealthCheck = "/health",
                HealthCheckUI = "/healthchecks-ui"
            }
        }))
        .WithName("Root")
        .WithTags("Info")
        .WithDescription("Returns service information")
        .Produces<object>(StatusCodes.Status200OK);

        return app;
    }
}
