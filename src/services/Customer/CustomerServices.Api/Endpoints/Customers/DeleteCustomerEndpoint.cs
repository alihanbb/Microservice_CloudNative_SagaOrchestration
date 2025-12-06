using CustomerServices.Application.Customers.DeleteCustomer;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class DeleteCustomerEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/customers/{customerId:int}", HandleAsync)
            .WithName("DeleteCustomer")
            .WithTags("Customers")
            .WithDescription("Soft deletes a customer")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromBody] DeleteCustomerRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCustomerCommand(customerId, request.Reason);
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
