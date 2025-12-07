namespace CustomerServices.Domain.Aggregate;

public interface ICustomerEventStore
{
    Task AppendEventsAsync(
        int customerId,
        IEnumerable<CustomerDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CustomerDomainEvent>> GetEventsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CustomerDomainEvent>> GetEventsFromVersionAsync(
        int customerId,
        int fromVersion,
        CancellationToken cancellationToken = default);

    Task<int> GetCurrentVersionAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetSnapshotAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task SaveSnapshotAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}
