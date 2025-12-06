using CustomerServices.Application.Customers.GetCustomerHistory;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class GetCustomerHistoryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers/{customerId:int}/history", HandleAsync)
            .WithName("GetCustomerHistory")
            .WithTags("Event Sourcing")
            .WithDescription("Gets the event history for a customer (Event Sourcing)")
            .WithOpenApi()
            .Produces<ApiResponse<CustomerHistoryResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerHistoryQuery(customerId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return Results.NotFound(result.ToApiResponse());

        return Results.Ok(result.ToApiResponse());
    }
}
