namespace CustomerServices.Domain.Events;

public abstract record CustomerDomainEvent : INotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int Version { get; init; }
    public int CustomerId { get; init; }
}
