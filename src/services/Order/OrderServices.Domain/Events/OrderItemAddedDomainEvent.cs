using MediatR;

namespace OrderServices.Domain.Events;

public record OrderItemAddedDomainEvent(
    Guid OrderId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice) : INotification;
