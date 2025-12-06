using CustomerServices.Domain.Aggregate;
using CustomerServices.Infra.Repositories;

namespace CustomerServices.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddApplication();

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<ICustomerEventStore, CustomerEventStore>();

        return services;
    }
}
