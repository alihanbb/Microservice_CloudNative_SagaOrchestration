using CustomerServices.Infra.EventSourcing;
using CustomerServices.Infra.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerServices.Infra;

/// <summary>
/// Extension methods for registering Infrastructure layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext
        services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        // Register repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Register Event Store
        services.AddScoped<ICustomerEventStore, CustomerEventStore>();

        return services;
    }

    /// <summary>
    /// Registers Infrastructure services with custom DbContext configuration
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<CustomerDbContext>(optionsAction);

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerEventStore, CustomerEventStore>();

        return services;
    }
}
