using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Azure SQL Database Configuration
// ========================================
builder.AddSqlServerDbContext<CustomerDbContext>("customerdb",
    configureDbContextOptions: options =>
    {
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
        options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

// ========================================
// Azure Key Vault Configuration
// ========================================
builder.AddAzureKeyVaultClient("keyvault");

// ========================================
// Health Checks
// ========================================
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"),
        tags: new[] { "api", "ready" })
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(false);
        var threshold = 1024L * 1024L * 1024L; // 1 GB
        return allocated < threshold
            ? HealthCheckResult.Healthy($"Memory: {allocated / 1024 / 1024} MB")
            : HealthCheckResult.Degraded($"Memory high: {allocated / 1024 / 1024} MB");
    }, tags: new[] { "api", "memory" })
    .AddDbContextCheck<CustomerDbContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "database", "sqlserver", "ready" });

// ========================================
// Add services to the container
// ========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Customer Service API",
        Version = "v1",
        Description = "Customer microservice with Azure SQL Database and Azure Key Vault integration",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Customer Service Team",
            Email = "customerservice@example.com"
        }
    });
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========================================
// Database Migration (Development)
// ========================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    
    try
    {
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("? Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "? Error applying database migrations");
    }
}

// ========================================
// Configure the HTTP request pipeline
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer Service API V1");
        options.RoutePrefix = "swagger";
    });
}

// Health Checks Endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api")
});

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("?? Customer Service API started successfully");
app.Logger.LogInformation("?? Health Checks: /health, /ready, /live");
app.Logger.LogInformation("?? Swagger UI: /swagger");
app.Logger.LogInformation("??? SQL Server Database: customerdb");

app.Run();

// Placeholder DbContext (should be replaced with actual implementation)
public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }
    
    // Add your DbSets here
    // public DbSet<Customer> Customers { get; set; }
}

