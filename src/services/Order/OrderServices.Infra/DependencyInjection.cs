using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace OrderServices.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        CosmosClient cosmosClient,
        string databaseName,
        string containerName)
    {
        services.AddSingleton(cosmosClient);
        services.AddScoped<IOrderRepository>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var container = client.GetContainer(databaseName, containerName);
            return new CosmosOrderRepository(container);
        });

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Container container)
    {
        services.AddScoped<IOrderRepository>(_ => new CosmosOrderRepository(container));
        return services;
    }
}
