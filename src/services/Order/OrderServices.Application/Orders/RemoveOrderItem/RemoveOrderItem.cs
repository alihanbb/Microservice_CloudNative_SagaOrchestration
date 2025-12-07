namespace OrderServices.Application.Orders.RemoveOrderItem;

#region Command

/// <summary>
/// Command to remove an item from an existing order
/// </summary>
public sealed record RemoveOrderItemCommand(
    int OrderId,
    Guid ProductId) : ICommand;

#endregion

#region Validator

/// <summary>
/// Validator for RemoveOrderItemCommand
/// </summary>
public sealed class RemoveOrderItemCommandValidator : AbstractValidator<RemoveOrderItemCommand>
{
    public RemoveOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for RemoveOrderItemCommand
/// Removes an item from an existing order (only if order is in Pending status)
/// </summary>
public sealed class RemoveOrderItemCommandHandler : ICommandHandler<RemoveOrderItemCommand>
{
    private readonly IOrderRepository _orderRepository;

    public RemoveOrderItemCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        RemoveOrderItemCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure($"Order with ID {request.OrderId} was not found");
        }

        try
        {
            // Domain aggregate handles validation and business rules
            order.RemoveOrderItem(request.ProductId);

            // Update order in CosmosDB
            await _orderRepository.UpdateAsync(order, cancellationToken);

            return Result.Success();
        }
        catch (OrderDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

#endregion
