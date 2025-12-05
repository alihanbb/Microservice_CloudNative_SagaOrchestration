namespace OrderServices.Api.Contracts.Requests;

public sealed record CreateOrderRequest(
    Guid CustomerId,
    string CustomerName,
    List<OrderItemRequest> Items);

public sealed record OrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record CancelOrderRequest(string Reason);

public sealed record UpdateOrderStatusRequest(string Status);

public sealed record AddOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
