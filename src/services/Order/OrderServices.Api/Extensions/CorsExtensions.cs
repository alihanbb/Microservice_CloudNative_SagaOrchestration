namespace OrderServices.Api.Extensions;

/// <summary>
/// Extension methods for CORS configuration
/// </summary>
public static class CorsExtensions
{
    public const string DefaultPolicyName = "DefaultCorsPolicy";
    public const string AllowAllPolicyName = "AllowAll";

    /// <summary>
    /// Adds CORS services with default configuration
    /// </summary>
    public static IServiceCollection AddOrderServiceCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Default policy - Allow all (Development)
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Named policy for specific origins (Production)
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                policy.WithOrigins(
                        "https://localhost:3000",
                        "https://localhost:5001")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds CORS services with custom origins
    /// </summary>
    public static IServiceCollection AddOrderServiceCors(
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
