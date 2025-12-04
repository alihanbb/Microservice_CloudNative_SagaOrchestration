using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductService.Api.Services;
using ProductService.Api.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// MongoDB Configuration
// ========================================
builder.Services.Configure<MongoDbConfiguration>(
    builder.Configuration.GetSection(MongoDbConfiguration.SectionName));

builder.AddMongoDBClient("mongodb");

builder.Services.AddHostedService<MongoDbInitializer>();

// ========================================
// Azure Key Vault Configuration
// ========================================
builder.AddAzureKeyVaultClient("keyvault");

// ========================================
// Health Checks Configuration
// ========================================
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"),
        tags: new[] { "api", "ready" })
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(false);
        var threshold = 1024L * 1024L * 1024L;
        return allocated < threshold
            ? HealthCheckResult.Healthy($"Memory: {allocated / 1024 / 1024} MB")
            : HealthCheckResult.Degraded($"Memory high: {allocated / 1024 / 1024} MB");
    }, tags: new[] { "api", "memory" })
    .AddMongoDb(
        sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>(),
        name: "mongodb",
        tags: new[] { "db", "mongodb", "ready" });

// ========================================
// API Services Configuration
// ========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Product Service API",
        Version = "v1",
        Description = "Product microservice with MongoDB and Azure Key Vault integration",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Product Service Team",
            Email = "productservice@example.com"
        }
    });
});

// ========================================
// CORS Configuration
// ========================================
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
// Configure the HTTP request pipeline
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API V1");
        options.RoutePrefix = "swagger";
    });
}

// ========================================
// Health Checks Endpoints
// ========================================
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

// ========================================
// Middleware Configuration
// ========================================
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ========================================
// Startup Logs
// ========================================
app.Logger.LogInformation("🚀 Product Service API started successfully");
app.Logger.LogInformation("📊 Health Checks: /health, /ready, /live");
app.Logger.LogInformation("📋 Swagger UI: /swagger");
app.Logger.LogInformation("🗄️ MongoDB Database: productdb");

app.Run();


