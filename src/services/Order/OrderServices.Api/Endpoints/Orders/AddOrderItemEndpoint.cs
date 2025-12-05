using OrderServices.Api.Contracts.Requests;
using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class AddOrderItemEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId:int}/items", HandleAsync)
            .WithName("AddOrderItem")
            .WithTags("Order Items")
            .WithDescription("Adds a new item to an existing order (only for pending orders)")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int orderId,
        [FromBody] AddOrderItemRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new AddOrderItemCommand(
            orderId,
            request.ProductId,
            request.ProductName,
            request.Quantity,
            request.UnitPrice);

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
