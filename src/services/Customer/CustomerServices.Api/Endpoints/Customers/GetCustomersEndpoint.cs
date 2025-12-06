using CustomerServices.Application.Customers.GetCustomers;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class GetCustomersEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers", HandleAsync)
            .WithName("GetCustomers")
            .WithTags("Customers")
            .WithDescription("Gets customers with pagination and filtering")
            .WithOpenApi()
            .Produces<ApiResponse<PagedCustomerResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IMediator mediator = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCustomersQuery(status, search, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return Results.BadRequest(result.ToApiResponse());

        return Results.Ok(result.ToApiResponse());
    }
}
