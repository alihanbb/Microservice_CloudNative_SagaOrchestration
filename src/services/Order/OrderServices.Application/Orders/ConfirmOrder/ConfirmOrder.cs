using OrderServices.Application.Common;
using OrderServices.Application.Common.Interfaces;

namespace OrderServices.Application.Orders.ConfirmOrder;

#region Command

/// <summary>
/// Command to confirm a pending order
/// </summary>
public sealed record ConfirmOrderCommand(int OrderId) : ICommand;

#endregion

#region Validator

/// <summary>
/// Validator for ConfirmOrderCommand
/// </summary>
public sealed class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for ConfirmOrderCommand
/// Transitions order from Pending to Confirmed status
/// </summary>
public sealed class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand>
{
    private readonly IOrderRepository _orderRepository;

    public ConfirmOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        ConfirmOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure($"Order with ID {request.OrderId} was not found");
        }

        try
        {
            // Domain aggregate handles the business logic
            order.ConfirmOrder();
            
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
