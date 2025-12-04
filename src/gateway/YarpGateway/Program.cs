using YarpGateway.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Service Discovery Configuration
// ========================================
builder.Services.AddServiceDiscovery();

// ========================================
// YARP Reverse Proxy Configuration
// ========================================
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ========================================
// Azure Key Vault Configuration
// ========================================
builder.AddAzureKeyVaultClient("keyvault");

// ========================================
// Health Checks Configuration
// ========================================
builder.Services.AddGatewayHealthChecks(builder.Configuration);
builder.Services.AddGatewayHealthChecksUI();

// ========================================
// API Services Configuration
// ========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Gateway",
        Version = "v1",
        Description = "YARP-based API Gateway with Service Discovery and Health Checks",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Gateway Team",
            Email = "gateway@example.com"
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

// ========================================
// Logging Configuration
// ========================================
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ========================================
// Configure the HTTP request pipeline
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway V1");
        options.RoutePrefix = "swagger";
    });
}

// ========================================
// Health Checks Endpoints
// ========================================
app.UseGatewayHealthChecks();

// ========================================
// Middleware Configuration
// ========================================
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();

// ========================================
// YARP Reverse Proxy
// ========================================
app.MapReverseProxy();

app.MapControllers();

// ========================================
// Startup Logs
// ========================================
app.Logger.LogInformation("?? API Gateway started successfully");
app.Logger.LogInformation("?? Health Checks: /health, /ready, /live");
app.Logger.LogInformation("?? Health Checks UI: /healthchecks-ui");
app.Logger.LogInformation("?? Swagger UI: /swagger");
app.Logger.LogInformation("?? YARP Reverse Proxy configured for:");
app.Logger.LogInformation("   • Order Service: /api/orders/*");
app.Logger.LogInformation("   • Customer Service: /api/customers/*");
app.Logger.LogInformation("   • Product Service: /api/products/*");

app.Run();

