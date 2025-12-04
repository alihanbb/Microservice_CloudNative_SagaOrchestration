using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderServices.Api.Configuration;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace OrderServices.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddOrderServiceHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cosmosConfig = configuration.GetSection(CosmosDbConfiguration.SectionName)
            .Get<CosmosDbConfiguration>() ?? new CosmosDbConfiguration();

        var healthChecksBuilder = services.AddHealthChecks()
            // Self health check
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "api", "ready" })
            
            // Memory health check
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var threshold = 1024L * 1024L * 1024L; // 1 GB
                
                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory usage: {allocated / 1024 / 1024} MB")
                    : HealthCheckResult.Degraded($"Memory usage high: {allocated / 1024 / 1024} MB");
            }, tags: new[] { "api", "memory" });

        // Cosmos DB health check - Custom implementation
        if (!string.IsNullOrEmpty(cosmosConfig.DatabaseName))
        {
            healthChecksBuilder.AddCheck("cosmosdb", () =>
            {
                try
                {
                    // Simple health check - can be enhanced
                    return HealthCheckResult.Healthy("Cosmos DB configuration is valid");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Cosmos DB check failed", ex);
                }
            }, tags: new[] { "database", "cosmosdb", "ready" });
        }

        return services;
    }

    public static IServiceCollection AddOrderServiceHealthChecksUI(this IServiceCollection services)
    {
        services
            .AddHealthChecksUI(setup =>
            {
                // Add health check endpoint to monitor
                setup.AddHealthCheckEndpoint("OrderService API", "/health");
                
                // Configure evaluation frequency
                setup.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
                
                // Maximum history entries
                setup.MaximumHistoryEntriesPerEndpoint(50);
                
                // Configure UI
                setup.SetApiMaxActiveRequests(1);
            })
            .AddInMemoryStorage(); // Use in-memory storage for development

        return services;
    }

    public static IApplicationBuilder UseOrderServiceHealthChecks(this WebApplication app)
    {
        // Health check endpoint with detailed response
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Readiness probe - only critical checks
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Liveness probe - simple check
        app.MapHealthChecks("/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("api"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Health Checks UI
        app.MapHealthChecksUI(setup =>
        {
            setup.UIPath = "/healthchecks-ui"; // UI path
            setup.ApiPath = "/healthchecks-api"; // API path for UI data
        });

        return app;
    }
}

