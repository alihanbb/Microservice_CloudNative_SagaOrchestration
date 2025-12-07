using OrderServices.Application.Common;
using OrderServices.Application.Common.Interfaces;

namespace OrderServices.Application.Orders.AddOrderItem;

#region Command

/// <summary>
/// Command to add a new item to an existing order
/// </summary>
public sealed record AddOrderItemCommand(
    int OrderId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice) : ICommand;

#endregion

#region Validator

/// <summary>
/// Validator for AddOrderItemCommand
/// </summary>
public sealed class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than zero");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for AddOrderItemCommand
/// Adds an item to an existing order (only if order is in Pending status)
/// </summary>
public sealed class AddOrderItemCommandHandler : ICommandHandler<AddOrderItemCommand>
{
    private readonly IOrderRepository _orderRepository;

    public AddOrderItemCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        AddOrderItemCommand request,
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
            order.AddOrderItem(
                request.ProductId,
                request.ProductName,
                request.Quantity,
                request.UnitPrice);

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
