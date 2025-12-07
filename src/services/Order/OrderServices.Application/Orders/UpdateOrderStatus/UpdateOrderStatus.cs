namespace OrderServices.Application.Orders.UpdateOrderStatus;

#region Command

/// <summary>
/// Command to update order status through the workflow
/// Supports: Paid, Shipped, Delivered status transitions
/// </summary>
public sealed record UpdateOrderStatusCommand(
    int OrderId,
    OrderStatusType NewStatus) : ICommand;

/// <summary>
/// Enum representing valid status transitions
/// </summary>
public enum OrderStatusType
{
    Paid = 1,
    Shipped = 2,
    Delivered = 3
}

#endregion

#region Validator

/// <summary>
/// Validator for UpdateOrderStatusCommand
/// </summary>
public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("Invalid order status");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for UpdateOrderStatusCommand
/// Transitions order through valid status workflow
/// </summary>
public sealed class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand>
{
    private readonly IOrderRepository _orderRepository;

    public UpdateOrderStatusCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure($"Order with ID {request.OrderId} was not found");
        }

        try
        {
            // Apply status transition based on requested status
            switch (request.NewStatus)
            {
                case OrderStatusType.Paid:
                    order.SetPaidStatus();
                    break;
                case OrderStatusType.Shipped:
                    order.SetShippedStatus();
                    break;
                case OrderStatusType.Delivered:
                    order.SetDeliveredStatus();
                    break;
                default:
                    return Result.Failure("Invalid status transition requested");
            }

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
