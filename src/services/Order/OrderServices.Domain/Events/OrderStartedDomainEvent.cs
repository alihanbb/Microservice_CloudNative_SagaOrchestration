using MediatR;

namespace OrderServices.Domain.Events;

public record OrderStartedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    decimal TotalAmount,
    DateTime OrderDate) : INotification;
