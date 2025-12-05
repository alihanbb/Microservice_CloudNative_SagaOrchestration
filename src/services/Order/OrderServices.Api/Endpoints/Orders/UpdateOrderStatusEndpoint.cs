using OrderServices.Api.Contracts.Requests;
using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class UpdateOrderStatusEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/orders/{orderId:int}/status", HandleAsync)
            .WithName("UpdateOrderStatus")
            .WithTags("Orders")
            .WithDescription("Updates the status of an order (Paid, Shipped, Delivered)")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int orderId,
        [FromBody] UpdateOrderStatusRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Parse status string to enum
        if (!Enum.TryParse<OrderStatusType>(request.Status, true, out var statusType))
        {
            return Results.BadRequest(new ApiResponse(false, 
                $"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames<OrderStatusType>())}"));
        }

        var command = new UpdateOrderStatusCommand(orderId, statusType);
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
