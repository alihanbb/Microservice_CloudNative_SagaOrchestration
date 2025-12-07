using System.Net;
using System.Text.Json;

using CustomerServices.Domain.Exceptions;
using SharedLibrary.Exceptions;

namespace CustomerServices.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Validation Error", "One or more validation errors occurred",
                    validationEx.Errors.SelectMany(e => e.Value).ToList())),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse("Not Found", notFoundEx.Message, null)),

            CustomerDomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Domain Error", domainEx.Message, null)),

            ConcurrencyException concurrencyEx => (
                HttpStatusCode.Conflict,
                new ErrorResponse("Concurrency Conflict", concurrencyEx.Message, null)),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("Server Error", "An unexpected error occurred", null))
        };

        _logger.LogError(exception, "Exception: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
public record ErrorResponse(string Title, string Detail, List<string>? Errors);

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
