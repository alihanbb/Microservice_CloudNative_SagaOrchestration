using OrderServices.Api.Contracts.Responses;

namespace OrderServices.Api.Endpoints.Orders;

public sealed class GetOrdersByCustomerEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/customer/{customerId:guid}", HandleAsync)
            .WithName("GetOrdersByCustomer")
            .WithTags("Orders")
            .WithDescription("Gets all orders for a specific customer")
            .WithOpenApi()
            .Produces<ApiResponse<List<OrderSummaryResponse>>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid customerId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetOrdersByCustomerQuery(customerId);
        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result.ToApiResponse());
    }
}
