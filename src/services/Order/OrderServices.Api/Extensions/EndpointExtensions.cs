using System.Reflection;

namespace OrderServices.Api.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(Endpoints.IEndpoint)));

        foreach (var type in endpointTypes)
        {
            services.AddSingleton(typeof(Endpoints.IEndpoint), type);
        }

        return services;
    }

    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetServices<Endpoints.IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoints(app);
        }

        return app;
    }
}
