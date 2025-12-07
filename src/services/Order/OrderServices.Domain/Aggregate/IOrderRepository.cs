namespace OrderServices.Domain.Aggregate;

/// <summary>
/// Order repository interface for CosmosDB persistence
/// </summary>
public interface IOrderRepository
{
    Order Add(Order order);
    
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    
    void Update(Order order);
    
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    
    Task<Order?> GetAsync(int orderId, CancellationToken cancellationToken = default);
    
    Task<Order?> GetByCustomerAsync(Guid customerId, int orderId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int orderId, Guid customerId, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(int orderId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}
