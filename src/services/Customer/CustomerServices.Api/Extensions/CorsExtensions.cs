namespace CustomerServices.Api.Extensions;

public static class CorsExtensions
{
    public const string DefaultPolicyName = "DefaultCorsPolicy";

    public static IServiceCollection AddCustomerServiceCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Development
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Production
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                policy.WithOrigins(
                        "https://localhost:3000",
                        "https://localhost:5000",
                        "https://localhost:5001")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddCustomerServiceCors(
        this IServiceCollection services,
        params string[] allowedOrigins)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
