using CustomerServices.Application.Customers.UpdateCustomer;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class UpdateCustomerEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/customers/{customerId:int}", HandleAsync)
            .WithName("UpdateCustomer")
            .WithTags("Customers")
            .WithDescription("Updates customer information")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromBody] UpdateCustomerRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(
            customerId,
            request.FirstName,
            request.LastName,
            request.PhoneCountryCode,
            request.PhoneNumber,
            request.Street,
            request.City,
            request.State,
            request.Country,
            request.ZipCode,
            request.ExpectedVersion);

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return Results.NotFound(result.ToApiResponse());
            
            if (result.Error?.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) == true)
                return Results.Conflict(result.ToApiResponse());

            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
