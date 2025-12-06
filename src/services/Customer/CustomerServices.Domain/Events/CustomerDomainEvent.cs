namespace CustomerServices.Domain.Events;

/// <summary>
/// Base class for all Customer domain events
/// Contains common properties for event sourcing
/// </summary>
public abstract record CustomerDomainEvent : INotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int Version { get; init; }
    public int CustomerId { get; init; }
}
