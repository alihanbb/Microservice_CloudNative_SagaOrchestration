using CustomerServices.Api.Middleware;

namespace CustomerServices.Api.Extensions;

public static class CustomerServiceExtensions
{
    public static WebApplicationBuilder AddCustomerService(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("customerdb"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            
            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        builder.Services.AddApplicationServices();

        builder.Services.AddEndpoints();

        builder.Services.AddCustomerServiceHealthChecks();

        builder.Services.AddCustomerServiceSwagger();

        builder.Services.AddCustomerServiceCors();

        return builder;
    }

    public static async Task<WebApplication> UseCustomerServiceAsync(this WebApplication app)
    {
        app.UseGlobalExceptionHandler();

        app.UseCustomerServiceSwagger();

        if (app.Environment.IsDevelopment())
        {
            await app.MigrateDatabaseAsync();
        }

        app.UseCustomerServiceHealthChecks();
        app.UseCors();
        app.UseHttpsRedirection();

        return app;
    }

    private static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("? Customer database migrations applied successfully");
            await CustomerDbSeeder.SeedAsync(dbContext, app.Logger);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error applying Customer database migrations - seeding will be skipped");
       
        }
    }

    
}
