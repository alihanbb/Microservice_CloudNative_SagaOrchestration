using CustomerServices.Application.Customers.ChangeEmail;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class ChangeEmailEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/customers/{customerId:int}/email", HandleAsync)
            .WithName("ChangeEmail")
            .WithTags("Customers")
            .WithDescription("Changes customer email address")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromBody] ChangeEmailRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ChangeEmailCommand(customerId, request.NewEmail, request.ExpectedVersion);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return Results.NotFound(result.ToApiResponse());

            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
