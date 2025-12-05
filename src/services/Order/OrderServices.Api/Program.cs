using OrderServices.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddOrderService();

var app = builder.Build();

app.UseOrderService();

app.Run();




