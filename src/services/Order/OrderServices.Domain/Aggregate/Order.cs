using OrderServices.Domain.Events;
using OrderServices.Domain.Exceptions;
using OrderServices.Domain.SeedWork;

namespace OrderServices.Domain.Aggregate;

public class Order : OrderEntity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;    
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }

    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    // EF Core için gerekli
    protected Order() { }

    public Order(Guid customerId, string customerName)
    {
        if (customerId == Guid.Empty)
            throw new OrderDomainException("Customer ID cannot be empty");
        
        if (string.IsNullOrWhiteSpace(customerName))
            throw new OrderDomainException("Customer name cannot be empty");

        CustomerId = customerId;
        CustomerName = customerName;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        TotalAmount = 0;

        AddDomainEvent(new OrderStartedDomainEvent(
            Guid.NewGuid(),
            CustomerId, 
            CustomerName, 
            TotalAmount,
            OrderDate));
    }

    // Factory method for creating order with items
    public static Order CreateOrder(Guid customerId, string customerName, List<OrderItem> items)
    {
        var order = new Order(customerId, customerName);
        
        foreach (var item in items)
        {
            order.AddOrderItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
        }

        return order;
    }

    public void AddOrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException($"Cannot add items to order in {Status.Name} status");

        var existingItem = _orderItems.FirstOrDefault(i => i.ProductId == productId);
        
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var orderItem = new OrderItem(productId, productName, quantity, Guid.Empty, unitPrice);
            _orderItems.Add(orderItem);
            
            AddDomainEvent(new OrderItemAddedDomainEvent(
                Guid.NewGuid(),
                productId,
                productName,
                quantity,
                unitPrice));
        }

        RecalculateTotalAmount();
    }

    public void RemoveOrderItem(Guid productId)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException($"Cannot remove items from order in {Status.Name} status");

        var itemToRemove = _orderItems.FirstOrDefault(i => i.ProductId == productId);
        
        if (itemToRemove == null)
            throw new OrderDomainException($"Order item with product ID {productId} not found");

        _orderItems.Remove(itemToRemove);
        RecalculateTotalAmount();
    }

    public void UpdateOrderItemQuantity(Guid productId, int newQuantity)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException($"Cannot update items in order with {Status.Name} status");

        var item = _orderItems.FirstOrDefault(i => i.ProductId == productId);
        
        if (item == null)
            throw new OrderDomainException($"Order item with product ID {productId} not found");

        item.UpdateQuantity(newQuantity);
        RecalculateTotalAmount();
    }

    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException($"Cannot confirm order in {Status.Name} status");

        if (!_orderItems.Any())
            throw new OrderDomainException("Cannot confirm order without items");

        SetStatus(OrderStatus.Confirmed);
    }

    public void SetPaidStatus()
    {
        if (Status != OrderStatus.Confirmed)
            throw new OrderDomainException($"Cannot set paid status for order in {Status.Name} status");

        SetStatus(OrderStatus.Paid);
    }

    public void SetShippedStatus()
    {
        if (Status != OrderStatus.Paid)
            throw new OrderDomainException($"Cannot ship order in {Status.Name} status");

        SetStatus(OrderStatus.Shipped);
    }

    public void SetDeliveredStatus()
    {
        if (Status != OrderStatus.Shipped)
            throw new OrderDomainException($"Cannot set delivered status for order in {Status.Name} status");

        SetStatus(OrderStatus.Delivered);
    }

    public void CancelOrder(string reason)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            throw new OrderDomainException($"Cannot cancel order in {Status.Name} status");

        var oldStatus = Status.Name;
        Status = OrderStatus.Cancelled;

        AddDomainEvent(new OrderCancelledDomainEvent(Guid.NewGuid(), reason));
        AddDomainEvent(new OrderStatusChangedDomainEvent(Guid.NewGuid(), oldStatus, Status.Name));
    }

    private void SetStatus(OrderStatus newStatus)
    {
        if (Status == newStatus) return;

        var oldStatus = Status.Name;
        Status = newStatus;

        AddDomainEvent(new OrderStatusChangedDomainEvent(Guid.NewGuid(), oldStatus, newStatus.Name));
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _orderItems.Sum(item => item.GetTotalPrice());
    }

    public bool CanBeCancelled()
    {
        return Status != OrderStatus.Delivered && 
               Status != OrderStatus.Cancelled && 
               Status != OrderStatus.Refunded;
    }

    public bool IsInProgress()
    {
        return Status == OrderStatus.Confirmed || 
               Status == OrderStatus.Paid || 
               Status == OrderStatus.Shipped;
    }

    public bool IsCompleted()
    {
        return Status == OrderStatus.Delivered;
    }
}
