using System.Text.Json;

namespace CustomerServices.Infra.EventSourcing;

public class CustomerEventEntity
{
    public long Id { get; set; }
    public int CustomerId { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime OccurredOn { get; set; }
    public DateTime StoredAt { get; set; }

    private CustomerEventEntity() { }

    public static CustomerEventEntity FromDomainEvent(CustomerDomainEvent @event)
    {
        return new CustomerEventEntity
        {
            CustomerId = @event.CustomerId,
            EventId = @event.EventId,
            EventType = @event.GetType().AssemblyQualifiedName ?? @event.GetType().Name,
            EventData = JsonSerializer.Serialize(@event, @event.GetType()),
            Version = @event.Version,
            OccurredOn = @event.OccurredOn,
            StoredAt = DateTime.UtcNow
        };
    }

    public CustomerDomainEvent? ToDomainEvent()
    {
        var type = Type.GetType(EventType);
        if (type == null) return null;

        return JsonSerializer.Deserialize(EventData, type) as CustomerDomainEvent;
    }
}
