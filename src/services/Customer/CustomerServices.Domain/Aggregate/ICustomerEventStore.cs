namespace CustomerServices.Domain.Aggregate;

/// <summary>
/// Interface for Event Store - stores and retrieves domain events
/// Used for Event Sourcing pattern
/// </summary>
public interface ICustomerEventStore
{
    /// <summary>
    /// Appends events to the event store for a specific customer
    /// </summary>
    Task AppendEventsAsync(
        int customerId,
        IEnumerable<CustomerDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for a specific customer
    /// </summary>
    Task<IEnumerable<CustomerDomainEvent>> GetEventsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for a specific customer from a specific version
    /// </summary>
    Task<IEnumerable<CustomerDomainEvent>> GetEventsFromVersionAsync(
        int customerId,
        int fromVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version number for a customer
    /// </summary>
    Task<int> GetCurrentVersionAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a snapshot of customer state at a specific version
    /// </summary>
    Task<Customer?> GetSnapshotAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a snapshot of customer state for faster rebuilding
    /// </summary>
    Task SaveSnapshotAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}
