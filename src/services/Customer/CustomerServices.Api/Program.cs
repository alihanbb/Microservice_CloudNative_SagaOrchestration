using CustomerServices.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddCustomerService();

var app = builder.Build();

await app.UseCustomerServiceAsync();

app.Run();

