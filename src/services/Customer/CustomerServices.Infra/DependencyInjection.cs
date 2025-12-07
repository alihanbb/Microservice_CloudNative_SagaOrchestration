using CustomerServices.Infra.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerServices.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
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

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<ICustomerEventStore, CustomerEventStore>();

        return services;
    }

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
