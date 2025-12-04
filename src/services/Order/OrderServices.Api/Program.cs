using Microsoft.Azure.Cosmos;
using OrderServices.Api.Configuration;
using OrderServices.Api.Services;
using OrderServices.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CosmosDbConfiguration>(
    builder.Configuration.GetSection(CosmosDbConfiguration.SectionName));

builder.Services.Configure<KeyVaultConfiguration>(
    builder.Configuration.GetSection(KeyVaultConfiguration.SectionName));

builder.AddAzureCosmosClient("cosmos",
    configureClientOptions: clientOptions =>
    {
        clientOptions.SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };
        clientOptions.ConnectionMode = ConnectionMode.Direct;
        clientOptions.ConsistencyLevel = ConsistencyLevel.Session;
        
        if (builder.Environment.IsDevelopment())
        {
            clientOptions.HttpClientFactory = () =>
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(httpMessageHandler);
            };
        }
    });

builder.Services.AddHostedService<CosmosDbInitializer>();

builder.AddAzureKeyVaultClient("keyvault");

builder.Services.AddOrderServiceHealthChecks(builder.Configuration);
builder.Services.AddOrderServiceHealthChecksUI();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order Service API",
        Version = "v1",
        Description = "Order microservice with Cosmos DB, Azure Key Vault integration and Health Checks UI",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Order Service Team",
            Email = "orderservice@example.com"
        }
    });
});
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API V1");
        options.RoutePrefix = "swagger";
    });
}

app.UseOrderServiceHealthChecks();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("?? Order Service API started successfully");
app.Logger.LogInformation("?? Health Checks UI available at: /healthchecks-ui");
app.Logger.LogInformation("?? Swagger UI available at: /swagger");

app.Run();




