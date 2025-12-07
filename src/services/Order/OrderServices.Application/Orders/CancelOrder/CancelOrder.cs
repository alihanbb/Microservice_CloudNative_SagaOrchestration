namespace OrderServices.Application.Orders.CancelOrder;

#region Command

/// <summary>
/// Command to cancel an existing order
/// </summary>
public sealed record CancelOrderCommand(
    int OrderId,
    string Reason) : ICommand;

#endregion

#region Validator

/// <summary>
/// Validator for CancelOrderCommand
/// </summary>
public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for CancelOrderCommand
/// Uses domain aggregate to cancel the order (business rules enforced in domain)
/// </summary>
public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure($"Order with ID {request.OrderId} was not found");
        }

        if (!order.CanBeCancelled())
        {
            return Result.Failure($"Order in {order.Status.Name} status cannot be cancelled");
        }

        // Domain aggregate handles the business logic and raises domain events
        order.CancelOrder(request.Reason);
        
        // Update order in CosmosDB
        await _orderRepository.UpdateAsync(order, cancellationToken);

        return Result.Success();
    }
}

#endregion
