using OrderServices.Application.Common;
using OrderServices.Application.Common.Interfaces;

namespace OrderServices.Application.Orders.GetOrdersByCustomer;

#region Query

/// <summary>
/// Query to get all orders for a specific customer
/// </summary>
public sealed record GetOrdersByCustomerQuery(Guid CustomerId) : IQuery<List<OrderSummaryResponse>>;

#endregion

#region Response

/// <summary>
/// Summary response for order list
/// </summary>
public sealed record OrderSummaryResponse(
    int OrderId,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    int ItemCount);

#endregion

#region Validator

/// <summary>
/// Validator for GetOrdersByCustomerQuery
/// </summary>
public sealed class GetOrdersByCustomerQueryValidator : AbstractValidator<GetOrdersByCustomerQuery>
{
    public GetOrdersByCustomerQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");
    }
}

#endregion

#region Handler

/// <summary>
/// Handler for GetOrdersByCustomerQuery
/// Retrieves all orders for a customer
/// </summary>
public sealed class GetOrdersByCustomerQueryHandler : IQueryHandler<GetOrdersByCustomerQuery, List<OrderSummaryResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersByCustomerQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<List<OrderSummaryResponse>>> Handle(
        GetOrdersByCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetOrdersByCustomerAsync(request.CustomerId, cancellationToken);

        var response = orders.Select(o => new OrderSummaryResponse(
            o.Id,
            o.OrderDate,
            o.Status.Name,
            o.TotalAmount,
            o.OrderItems.Count)).ToList();

        return Result<List<OrderSummaryResponse>>.Success(response);
    }
}

#endregion
