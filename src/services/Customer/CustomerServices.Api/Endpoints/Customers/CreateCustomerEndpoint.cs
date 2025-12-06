using CustomerServices.Application.Customers.CreateCustomer;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class CreateCustomerEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customers", HandleAsync)
            .WithName("CreateCustomer")
            .WithTags("Customers")
            .WithDescription("Creates a new customer")
            .WithOpenApi()
            .Produces<ApiResponse<CreateCustomerResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateCustomerRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneCountryCode,
            request.PhoneNumber,
            request.Street,
            request.City,
            request.State,
            request.Country,
            request.ZipCode);

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Results.BadRequest(result.ToApiResponse());

        return Results.Created(
            $"/api/customers/{result.Value!.CustomerId}",
            result.ToApiResponse());
    }
}
