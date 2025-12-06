namespace CustomerServices.Infra;

/// <summary>
/// MediatR extension methods for dispatching domain events
/// </summary>
public static class MediatorExtension
{
    /// <summary>
    /// Dispatches all domain events from tracked entities
    /// </summary>
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, CustomerDbContext ctx)
    {
        var domainEntities = ctx.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents!)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent);
        }
    }
}
