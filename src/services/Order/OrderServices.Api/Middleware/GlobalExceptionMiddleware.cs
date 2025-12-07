using System.Net;
using System.Text.Json;
using SharedLibrary.Exceptions;
using OrderServices.Domain.Exceptions;

namespace OrderServices.Api.Middleware;

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
                new ErrorResponse(
                    "Validation Error",
                    "One or more validation errors occurred",
                    validationEx.Errors.SelectMany(e => e.Value).ToList())),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(
                    "Not Found",
                    notFoundEx.Message,
                    null)),

            OrderDomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(
                    "Domain Error",
                    domainEx.Message,
                    null)),

            ForbiddenAccessException => (
                HttpStatusCode.Forbidden,
                new ErrorResponse(
                    "Forbidden",
                    "You do not have permission to perform this action",
                    null)),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse(
                    "Server Error",
                    "An unexpected error occurred",
                    null))
        };

        _logger.LogError(exception, 
            "Exception occurred: {Message}. StatusCode: {StatusCode}", 
            exception.Message, 
            statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public record ErrorResponse(
    string Title,
    string Detail,
    List<string>? Errors);

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
