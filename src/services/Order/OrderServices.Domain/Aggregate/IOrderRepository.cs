
using OrderServices.Domain.SeedWork;

namespace OrderServices.Domain.Aggregate;

public interface IOrderRepository : IRepository<Order>
{
    Order Add(Order order);
    
    void Update(Order order);
    
    Task<Order?> GetAsync(int orderId, CancellationToken cancellationToken = default);
    
    Task<Order?> GetByCustomerAsync(Guid customerId, int orderId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
}
