using OrderServices.Api.Middleware;
using OrderServices.Application;
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
        IConfiguration configuration)
    {
        // Application Layer (MediatR, FluentValidation, Behaviors)
        services.AddApplication();

        // Infrastructure Layer (DbContext, Repositories)
        var connectionString = configuration.GetConnectionString("OrderDb")
            ?? "Server=(localdb)\\mssqllocaldb;Database=OrderDb;Trusted_Connection=True;";
        services.AddInfrastructure(connectionString);

        // Minimal API Endpoints
        services.AddEndpoints();

        return services;
    }

    /// <summary>
    /// Adds all application services with custom connection string
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddApplication();
        services.AddInfrastructure(connectionString);
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
