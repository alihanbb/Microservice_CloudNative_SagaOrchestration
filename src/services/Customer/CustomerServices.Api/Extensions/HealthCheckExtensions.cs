using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerServices.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomerServiceHealthChecks(this IServiceCollection services)
    {
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
            .AddDbContextCheck<CustomerDbContext>(
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sqlserver", "ready"]);

        return services;
    }

    public static WebApplication UseCustomerServiceHealthChecks(this WebApplication app)
    {
        app.MapEndpoints();

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
