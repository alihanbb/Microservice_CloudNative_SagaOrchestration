using Microsoft.Extensions.Logging;
using OrderServices.Domain.Events;

namespace OrderServices.Application.Orders.DomainEventHandlers;

/// <summary>
/// Handles OrderStartedDomainEvent
/// Can trigger integration events, notifications, etc.
/// </summary>
public sealed class OrderStartedDomainEventHandler : INotificationHandler<OrderStartedDomainEvent>
{
    private readonly ILogger<OrderStartedDomainEventHandler> _logger;

    public OrderStartedDomainEventHandler(ILogger<OrderStartedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderStartedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order started - OrderId: {OrderId}, CustomerId: {CustomerId}, CustomerName: {CustomerName}, TotalAmount: {TotalAmount}",
            notification.OrderId,
            notification.CustomerId,
            notification.CustomerName,
            notification.TotalAmount);

        // Here you can:
        // - Publish integration events to message broker
        // - Send notifications
        // - Update read models
        // - Trigger other workflows

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles OrderCancelledDomainEvent
/// </summary>
public sealed class OrderCancelledDomainEventHandler : INotificationHandler<OrderCancelledDomainEvent>
{
    private readonly ILogger<OrderCancelledDomainEventHandler> _logger;

    public OrderCancelledDomainEventHandler(ILogger<OrderCancelledDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order cancelled - OrderId: {OrderId}, Reason: {Reason}, CancelledAt: {CancelledAt}",
            notification.OrderId,
            notification.Reason,
            notification.CancelledAt);

        // Here you can:
        // - Trigger refund process
        // - Release inventory
        // - Send cancellation notification
        // - Publish OrderCancelledIntegrationEvent

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles OrderStatusChangedDomainEvent
/// </summary>
public sealed class OrderStatusChangedDomainEventHandler : INotificationHandler<OrderStatusChangedDomainEvent>
{
    private readonly ILogger<OrderStatusChangedDomainEventHandler> _logger;

    public OrderStatusChangedDomainEventHandler(ILogger<OrderStatusChangedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order status changed - OrderId: {OrderId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}, ChangedAt: {ChangedAt}",
            notification.OrderId,
            notification.OldStatus,
            notification.NewStatus,
            notification.ChangedAt);

        // Here you can:
        // - Update tracking information
        // - Send status update notification
        // - Trigger downstream processes based on new status

        return Task.CompletedTask;
    }
}
