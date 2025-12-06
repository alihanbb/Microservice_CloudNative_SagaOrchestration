using CustomerServices.Application.Customers.GetCustomerById;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class GetCustomerByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers/{customerId:int}", HandleAsync)
            .WithName("GetCustomerById")
            .WithTags("Customers")
            .WithDescription("Gets a customer by ID")
            .WithOpenApi()
            .Produces<ApiResponse<CustomerDetailResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerByIdQuery(customerId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return Results.NotFound(result.ToApiResponse());

        return Results.Ok(result.ToApiResponse());
    }
}
