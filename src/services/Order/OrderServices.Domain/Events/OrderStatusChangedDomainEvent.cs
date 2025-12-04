using MediatR;

namespace OrderServices.Domain.Events;

public record OrderStatusChangedDomainEvent(
    Guid OrderId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt) : INotification
{
    public OrderStatusChangedDomainEvent(Guid orderId, string oldStatus, string newStatus)
        : this(orderId, oldStatus, newStatus, DateTime.UtcNow)
    {
    }
}
