namespace OrderServices.Infra;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Order Add(Order order)
    {
        return _context.Orders.Add(order).Entity;
    }

    public void Update(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
    }

    public async Task<Order?> GetAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order != null)
        {
            await _context.Entry(order)
                .Reference(o => o.Status)
                .LoadAsync(cancellationToken);
        }

        return order;
    }

    public async Task<Order?> GetByCustomerAsync(Guid customerId, int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.Status.Id == status.Id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public void Delete(Order order)
    {
        _context.Orders.Remove(order);
    }

    public async Task<bool> ExistsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.AnyAsync(o => o.Id == orderId, cancellationToken);
    }
}
