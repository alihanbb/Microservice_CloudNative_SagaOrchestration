using OrderServices.Api.Contracts.Requests;
using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class CreateOrderEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders", HandleAsync)
            .WithName("CreateOrder")
            .WithTags("Orders")
            .WithDescription("Creates a new order with the specified items")
            .WithOpenApi()
            .Produces<ApiResponse<CreateOrderResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateOrderRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.CustomerId,
            request.CustomerName,
            request.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice)).ToList());

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Created(
            $"/api/orders/{result.Value!.OrderId}",
            result.ToApiResponse());
    }
}
