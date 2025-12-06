using Microsoft.OpenApi.Models;

namespace CustomerServices.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomerServiceSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Customer Service API",
                Version = "v1",
                Description = "Customer microservice with DDD, CQRS, Event Sourcing pattern",
                Contact = new OpenApiContact
                {
                    Name = "Customer Service Team",
                    Email = "customerservice@example.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

        });

        return services;
    }

    public static WebApplication UseCustomerServiceSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer Service API V1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Customer Service API";
                options.DisplayRequestDuration();
            });
        }

        return app;
    }
}
