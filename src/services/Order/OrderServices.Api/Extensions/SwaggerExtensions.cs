using Microsoft.OpenApi.Models;

namespace OrderServices.Api.Extensions;

/// <summary>
/// Extension methods for Swagger/OpenAPI configuration
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI services with Order Service configuration
    /// </summary>
    public static IServiceCollection AddOrderServiceSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Order Service API",
                Version = "v1",
                Description = "Order microservice with DDD, CQRS, Minimal API pattern",
                Contact = new OpenApiContact
                {
                    Name = "Order Service Team",
                    Email = "orderservice@example.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Custom schema IDs to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Add security definition if needed
            // options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
        });

        return services;
    }

    /// <summary>
    /// Uses Swagger middleware with Order Service configuration
    /// </summary>
    public static WebApplication UseOrderServiceSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API V1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Order Service API";
                options.DisplayRequestDuration();
            });
        }

        return app;
    }
}
