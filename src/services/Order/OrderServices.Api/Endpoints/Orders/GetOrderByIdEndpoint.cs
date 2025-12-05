using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class GetOrderByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{orderId:int}", HandleAsync)
            .WithName("GetOrderById")
            .WithTags("Orders")
            .WithDescription("Gets an order by its ID")
            .WithOpenApi()
            .Produces<ApiResponse<OrderDetailResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int orderId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(orderId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
