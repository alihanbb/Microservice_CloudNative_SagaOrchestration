using MediatR;

namespace OrderServices.Domain.Events;

public record OrderCancelledDomainEvent(
    Guid OrderId,
    string Reason,
    DateTime CancelledAt) : INotification
{
    public OrderCancelledDomainEvent(Guid orderId, string reason)
        : this(orderId, reason, DateTime.UtcNow)
    {
    }
}
