using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerServices.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomerServiceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("customerdb") 
            ?? throw new InvalidOperationException("Connection string 'customerdb' not found.");

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Customer API is running"),
                tags: ["api", "ready", "live"])
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(false);
                var threshold = 1024L * 1024L * 1024L; 
                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory: {allocated / 1024 / 1024} MB")
                    : HealthCheckResult.Degraded($"Memory high: {allocated / 1024 / 1024} MB");
            }, tags: ["api", "memory"])
            .AddSqlServer(
                connectionString: connectionString,
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sqlserver", "ready"])
            .AddDbContextCheck<CustomerDbContext>(
                name: "ef-core-customerdb",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "ef-core", "ready"]);

        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(30);
            options.MaximumHistoryEntriesPerEndpoint(50); // Endpoint baþýna maksimum 50 geçmiþ kaydý
            options.SetApiMaxActiveRequests(1); // Ayný anda maksimum 1 istek

            options.AddHealthCheckEndpoint("Customer API", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }

    public static WebApplication UseCustomerServiceHealthChecks(this WebApplication app)
    {
        app.MapEndpoints();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-ui-api"; 
        });

        app.MapGet("/", () => Results.Ok(new
        {
            Service = "Customer Service API",
            Version = "v1",
            Status = "Running",
            Architecture = "DDD + CQRS + Event Sourcing",
            Timestamp = DateTime.UtcNow,
            Endpoints = new
            {
                Documentation = "/swagger",
                Health = "/health",
                HealthUI = "/health-ui",
                Ready = "/ready",
                Live = "/live",
                Customers = "/api/customers"
            }
        }))
        .WithName("Root")
        .WithTags("Info")
        .Produces<object>(StatusCodes.Status200OK);

        return app;
    }
}
