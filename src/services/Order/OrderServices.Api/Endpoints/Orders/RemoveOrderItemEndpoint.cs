using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class RemoveOrderItemEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/orders/{orderId:int}/items/{productId:guid}", HandleAsync)
            .WithName("RemoveOrderItem")
            .WithTags("Order Items")
            .WithDescription("Removes an item from an existing order (only for pending orders)")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int orderId,
        [FromRoute] Guid productId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RemoveOrderItemCommand(orderId, productId);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.NotFound(result.ToApiResponse());
            }
            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
