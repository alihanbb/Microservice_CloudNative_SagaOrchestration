using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace YarpGateway.Extensions;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Gateway is running"),
                tags: new[] { "gateway", "ready" })
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(false);
                var threshold = 1024L * 1024L * 1024L; // 1 GB
                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory: {allocated / 1024 / 1024} MB")
                    : HealthCheckResult.Degraded($"Memory high: {allocated / 1024 / 1024} MB");
            }, tags: new[] { "gateway", "memory" });

        return services;
    }

    public static IServiceCollection AddGatewayHealthChecksUI(this IServiceCollection services)
    {
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30);
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("Gateway Health", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }

    public static IApplicationBuilder UseGatewayHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.ToString()
                    }),
                    totalDuration = report.TotalDuration.ToString()
                });
                await context.Response.WriteAsync(result);
            }
        });

        app.UseHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.UseHealthChecks("/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("gateway")
        });

        app.UseHealthChecksUI(options =>
        {
            options.UIPath = "/healthchecks-ui";
            options.ApiPath = "/healthchecks-api";
        });

        return app;
    }
}
