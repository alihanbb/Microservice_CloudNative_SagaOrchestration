using OrderServices.Application.Common;
using OrderServices.Application.Common.Interfaces;

namespace OrderServices.Application.Orders.GetOrderById;

#region Query

/// <summary>
/// Query to get an order by its ID
/// </summary>
public sealed record GetOrderByIdQuery(int OrderId) : IQuery<OrderDetailResponse>;

#endregion

#region Response

/// <summary>
/// Detailed order response including all items
/// </summary>
public sealed record OrderDetailResponse(
    int OrderId,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    bool CanBeCancelled,
    bool IsInProgress,
    bool IsCompleted,
    List<OrderItemDetailResponse> Items);

/// <summary>
/// Order item detail in the response
/// </summary>
public sealed record OrderItemDetailResponse(
    int ItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

#endregion

#region Validator

/// <summary>
/// Validator for GetOrderByIdQuery
/// </summary>
public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Order ID must be greater than zero");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for GetOrderByIdQuery
/// Retrieves order details from the repository
/// </summary>
public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDetailResponse>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDetailResponse>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result<OrderDetailResponse>.Failure($"Order with ID {request.OrderId} was not found");
        }

        var response = new OrderDetailResponse(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.OrderDate,
            order.Status.Name,
            order.TotalAmount,
            order.CanBeCancelled(),
            order.IsInProgress(),
            order.IsCompleted(),
            order.OrderItems.Select(i => new OrderItemDetailResponse(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.GetTotalPrice())).ToList());

        return Result<OrderDetailResponse>.Success(response);
    }
}

#endregion
