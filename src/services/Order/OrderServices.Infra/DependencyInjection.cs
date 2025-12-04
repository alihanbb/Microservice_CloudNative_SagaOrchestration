using Microsoft.Extensions.DependencyInjection;

namespace OrderServices.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<OrderDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<IRequestManager, RequestManager>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<OrderDbContext>(optionsAction);

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<IRequestManager, RequestManager>();

        return services;
    }
}
