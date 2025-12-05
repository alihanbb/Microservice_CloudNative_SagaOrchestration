using OrderServices.Application.Common;
using OrderServices.Application.Common.Interfaces;

namespace OrderServices.Application.Orders.GetOrdersByStatus;

#region Query

/// <summary>
/// Query to get all orders with a specific status
/// </summary>
public sealed record GetOrdersByStatusQuery(string StatusName) : IQuery<List<OrderByStatusResponse>>;

#endregion

#region Response

/// <summary>
/// Order response for status-based queries
/// </summary>
public sealed record OrderByStatusResponse(
    int OrderId,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDate,
    decimal TotalAmount,
    int ItemCount);

#endregion

#region Validator

/// <summary>
/// Validator for GetOrdersByStatusQuery
/// </summary>
public sealed class GetOrdersByStatusQueryValidator : AbstractValidator<GetOrdersByStatusQuery>
{
    private static readonly string[] ValidStatuses = 
    {
        "Pending", "Confirmed", "Paid", "Shipped", "Delivered", "Cancelled", "Refunded"
    };

    public GetOrdersByStatusQueryValidator()
    {
        RuleFor(x => x.StatusName)
            .NotEmpty()
            .WithMessage("Status name is required")
            .Must(status => ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for GetOrdersByStatusQuery
/// Retrieves all orders with the specified status
/// </summary>
public sealed class GetOrdersByStatusQueryHandler : IQueryHandler<GetOrdersByStatusQuery, List<OrderByStatusResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersByStatusQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<List<OrderByStatusResponse>>> Handle(
        GetOrdersByStatusQuery request,
        CancellationToken cancellationToken)
    {
        var status = OrderStatus.FromName(request.StatusName);
        var orders = await _orderRepository.GetOrdersByStatusAsync(status, cancellationToken);

        var response = orders.Select(o => new OrderByStatusResponse(
            o.Id,
            o.CustomerId,
            o.CustomerName,
            o.OrderDate,
            o.TotalAmount,
            o.OrderItems.Count)).ToList();

        return Result<List<OrderByStatusResponse>>.Success(response);
    }
}

#endregion
