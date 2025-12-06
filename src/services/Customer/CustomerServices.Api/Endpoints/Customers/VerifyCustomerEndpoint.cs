using CustomerServices.Application.Customers.VerifyCustomer;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class VerifyCustomerEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customers/{customerId:int}/verify", HandleAsync)
            .WithName("VerifyCustomer")
            .WithTags("Customers")
            .WithDescription("Verifies a customer account")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new VerifyCustomerCommand(customerId);
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
