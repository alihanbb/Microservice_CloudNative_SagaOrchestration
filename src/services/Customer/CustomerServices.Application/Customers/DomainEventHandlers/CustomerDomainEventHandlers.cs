using Microsoft.Extensions.Logging;

namespace CustomerServices.Application.Customers.DomainEventHandlers;

/// <summary>
/// Handles CustomerCreatedDomainEvent
/// </summary>
public sealed class CustomerCreatedDomainEventHandler : INotificationHandler<CustomerCreatedDomainEvent>
{
    private readonly ILogger<CustomerCreatedDomainEventHandler> _logger;

    public CustomerCreatedDomainEventHandler(ILogger<CustomerCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer created - CustomerId: {CustomerId}, Name: {FirstName} {LastName}, Email: {Email}",
            notification.CustomerId,
            notification.FirstName,
            notification.LastName,
            notification.Email);

        // Here you can:
        // - Send welcome email
        // - Create related records in other services
        // - Publish integration event

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles CustomerVerifiedDomainEvent
/// </summary>
public sealed class CustomerVerifiedDomainEventHandler : INotificationHandler<CustomerVerifiedDomainEvent>
{
    private readonly ILogger<CustomerVerifiedDomainEventHandler> _logger;

    public CustomerVerifiedDomainEventHandler(ILogger<CustomerVerifiedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(CustomerVerifiedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer verified - CustomerId: {CustomerId}, VerifiedAt: {VerifiedAt}",
            notification.CustomerId,
            notification.VerifiedAt);

        // Send verification confirmation email
        // Update external systems

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles CustomerStatusChangedDomainEvent
/// </summary>
public sealed class CustomerStatusChangedDomainEventHandler : INotificationHandler<CustomerStatusChangedDomainEvent>
{
    private readonly ILogger<CustomerStatusChangedDomainEventHandler> _logger;

    public CustomerStatusChangedDomainEventHandler(ILogger<CustomerStatusChangedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(CustomerStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer status changed - CustomerId: {CustomerId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}, Reason: {Reason}",
            notification.CustomerId,
            notification.OldStatus,
            notification.NewStatus,
            notification.Reason);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles CustomerDeletedDomainEvent
/// </summary>
public sealed class CustomerDeletedDomainEventHandler : INotificationHandler<CustomerDeletedDomainEvent>
{
    private readonly ILogger<CustomerDeletedDomainEventHandler> _logger;

    public CustomerDeletedDomainEventHandler(ILogger<CustomerDeletedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(CustomerDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer deleted - CustomerId: {CustomerId}, Reason: {Reason}, DeletedAt: {DeletedAt}",
            notification.CustomerId,
            notification.Reason,
            notification.DeletedAt);

        // Anonymize customer data
        // Notify related services
        // Cancel subscriptions

        return Task.CompletedTask;
    }
}
