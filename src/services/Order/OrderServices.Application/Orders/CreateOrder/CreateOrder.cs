using OrderServices.Application.Common;
using OrderServices.Application.Common.Interfaces;

namespace OrderServices.Application.Orders.CreateOrder;

#region Command

/// <summary>
/// Command to create a new order
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string CustomerName,
    List<OrderItemDto> Items) : ICommand<CreateOrderResponse>;

/// <summary>
/// DTO for order items in the create command
/// </summary>
public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

#endregion

#region Response

/// <summary>
/// Response returned after creating an order
/// </summary>
public sealed record CreateOrderResponse(
    int OrderId,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    List<OrderItemResponse> Items);

/// <summary>
/// Order item in the response
/// </summary>
public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

#endregion

#region Validator

/// <summary>
/// Validator for CreateOrderCommand
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required")
            .MaximumLength(200)
            .WithMessage("Customer name cannot exceed 200 characters");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty()
                .WithMessage("Product ID is required");

            item.RuleFor(i => i.ProductName)
                .NotEmpty()
                .WithMessage("Product name is required")
                .MaximumLength(200)
                .WithMessage("Product name cannot exceed 200 characters");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero")
                .LessThanOrEqualTo(1000)
                .WithMessage("Quantity cannot exceed 1000");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Unit price must be greater than zero");
        });
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for CreateOrderCommand
/// Creates a new order with items using the domain aggregate
/// </summary>
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Create order using domain aggregate
        var order = new Order(request.CustomerId, request.CustomerName);

        // Add items to order (domain logic handles validation)
        foreach (var item in request.Items)
        {
            order.AddOrderItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice);
        }

        // Persist the order
        _orderRepository.Add(order);
        await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Map to response
        var response = new CreateOrderResponse(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.OrderDate,
            order.Status.Name,
            order.TotalAmount,
            order.OrderItems.Select(i => new OrderItemResponse(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.GetTotalPrice())).ToList());

        return Result<CreateOrderResponse>.Success(response);
    }
}

#endregion
