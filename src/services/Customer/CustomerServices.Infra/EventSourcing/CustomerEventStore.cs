using CustomerServices.Infra.EventSourcing;

namespace CustomerServices.Infra;

/// <summary>
/// Implementation of Event Store for Customer aggregate
/// Stores and retrieves domain events for Event Sourcing
/// </summary>
public class CustomerEventStore : ICustomerEventStore
{
    private readonly CustomerDbContext _context;
    private const int SnapshotInterval = 10; // Create snapshot every 10 events

    public CustomerEventStore(CustomerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Appends events to the event store with optimistic concurrency check
    /// </summary>
    public async Task AppendEventsAsync(
        int customerId,
        IEnumerable<CustomerDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = await GetCurrentVersionAsync(customerId, cancellationToken);

        if (currentVersion != expectedVersion)
        {
            throw new DbUpdateConcurrencyException(
                $"Concurrency conflict for customer {customerId}. Expected version {expectedVersion}, but found {currentVersion}");
        }

        var eventEntities = events.Select(CustomerEventEntity.FromDomainEvent).ToList();
        
        await _context.CustomerEvents.AddRangeAsync(eventEntities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Check if we need to create a snapshot
        var newVersion = eventEntities.Max(e => e.Version);
        if (newVersion % SnapshotInterval == 0)
        {
            var customer = await RebuildFromEventsAsync(customerId, cancellationToken);
            if (customer != null)
            {
                await SaveSnapshotAsync(customer, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Gets all events for a specific customer
    /// </summary>
    public async Task<IEnumerable<CustomerDomainEvent>> GetEventsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var eventEntities = await _context.CustomerEvents
            .Where(e => e.CustomerId == customerId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return eventEntities
            .Select(e => e.ToDomainEvent())
            .Where(e => e != null)
            .Cast<CustomerDomainEvent>();
    }

    /// <summary>
    /// Gets events from a specific version
    /// </summary>
    public async Task<IEnumerable<CustomerDomainEvent>> GetEventsFromVersionAsync(
        int customerId,
        int fromVersion,
        CancellationToken cancellationToken = default)
    {
        var eventEntities = await _context.CustomerEvents
            .Where(e => e.CustomerId == customerId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return eventEntities
            .Select(e => e.ToDomainEvent())
            .Where(e => e != null)
            .Cast<CustomerDomainEvent>();
    }

    /// <summary>
    /// Gets the current version number
    /// </summary>
    public async Task<int> GetCurrentVersionAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var maxVersion = await _context.CustomerEvents
            .Where(e => e.CustomerId == customerId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return maxVersion ?? 0;
    }

    /// <summary>
    /// Gets the latest snapshot for a customer
    /// </summary>
    public async Task<Customer?> GetSnapshotAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.CustomerSnapshots
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot == null)
            return null;

        // Rebuild from snapshot + events after snapshot
        var snapshotData = snapshot.ToSnapshotData();
        if (snapshotData == null)
            return null;

        // Get events after the snapshot
        var events = await GetEventsFromVersionAsync(customerId, snapshot.Version, cancellationToken);

        // If no events after snapshot, we need to rebuild from all events
        // This is a simplified approach - in production, you'd rebuild from snapshot data
        return await RebuildFromEventsAsync(customerId, cancellationToken);
    }

    /// <summary>
    /// Saves a snapshot of customer state
    /// </summary>
    public async Task SaveSnapshotAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        var snapshot = CustomerSnapshot.FromCustomer(customer);
        
        // Remove old snapshots (keep only the latest)
        var oldSnapshots = await _context.CustomerSnapshots
            .Where(s => s.CustomerId == customer.Id)
            .ToListAsync(cancellationToken);
        
        _context.CustomerSnapshots.RemoveRange(oldSnapshots);
        await _context.CustomerSnapshots.AddAsync(snapshot, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Rebuilds customer state from all events
    /// </summary>
    private async Task<Customer?> RebuildFromEventsAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        var events = await GetEventsAsync(customerId, cancellationToken);
        var eventList = events.ToList();

        if (!eventList.Any())
            return null;

        return Customer.FromHistory(eventList);
    }
}
