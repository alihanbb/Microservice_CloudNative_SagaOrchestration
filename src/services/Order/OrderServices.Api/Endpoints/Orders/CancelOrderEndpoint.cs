using OrderServices.Api.Contracts.Requests;
using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class CancelOrderEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId:int}/cancel", HandleAsync)
            .WithName("CancelOrder")
            .WithTags("Orders")
            .WithDescription("Cancels an existing order")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int orderId,
        [FromBody] CancelOrderRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CancelOrderCommand(orderId, request.Reason);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            // Check if it's a not found error
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.NotFound(result.ToApiResponse());
            }
            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
