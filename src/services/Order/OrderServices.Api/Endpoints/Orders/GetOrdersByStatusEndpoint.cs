using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class GetOrdersByStatusEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/status/{status}", HandleAsync)
            .WithName("GetOrdersByStatus")
            .WithTags("Orders")
            .WithDescription("Gets all orders with a specific status")
            .WithOpenApi()
            .Produces<ApiResponse<List<OrderByStatusResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] string status,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetOrdersByStatusQuery(status);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
