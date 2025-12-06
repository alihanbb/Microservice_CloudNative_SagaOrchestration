using CustomerServices.Api.Contracts.Requests;
using CustomerServices.Api.Contracts.Responses;
using CustomerServices.Application.Customers.ChangeStatus;

namespace CustomerServices.Api.Endpoints.Customers;

public sealed class ChangeStatusEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customers/{customerId:int}/status", HandleAsync)
            .WithName("ChangeStatus")
            .WithTags("Customers")
            .WithDescription("Changes customer status (Activate, Deactivate, Suspend)")
            .WithOpenApi()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] int customerId,
        [FromBody] ChangeStatusRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ChangeStatusCommand(customerId, request.Action, request.Reason);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return Results.NotFound(result.ToApiResponse());

            return Results.BadRequest(result.ToApiResponse());
        }

        return Results.Ok(result.ToApiResponse());
    }
}
