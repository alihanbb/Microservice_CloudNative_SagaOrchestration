using System.Net;
using OrderServices.Infra.Mappers;
using OrderServices.Infra.Models;

namespace OrderServices.Infra;

public class CosmosOrderRepository : IOrderRepository
{
    private readonly Container _container;

    public CosmosOrderRepository(Container container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public Order Add(Order order)
    {
        // Note: Actual creation happens in SaveChangesAsync pattern
        // For CosmosDB, we'll create immediately
        var document = OrderMapper.ToDocument(order);
        _container.CreateItemAsync(document, new PartitionKey(document.CustomerId)).GetAwaiter().GetResult();
        return order;
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var document = OrderMapper.ToDocument(order);
        await _container.CreateItemAsync(document, new PartitionKey(document.CustomerId), cancellationToken: cancellationToken);
        return order;
    }

    public void Update(Order order)
    {
        var document = OrderMapper.ToDocument(order);
        _container.ReplaceItemAsync(document, document.Id, new PartitionKey(document.CustomerId)).GetAwaiter().GetResult();
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var document = OrderMapper.ToDocument(order);
        await _container.ReplaceItemAsync(document, document.Id, new PartitionKey(document.CustomerId), cancellationToken: cancellationToken);
    }

    public async Task<Order?> GetAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id AND c.type = 'Order'")
            .WithParameter("@id", orderId.ToString());

        var iterator = _container.GetItemQueryIterator<OrderDocument>(query);
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            var document = response.FirstOrDefault();
            if (document != null)
            {
                return OrderMapper.ToDomain(document);
            }
        }

        return null;
    }

    public async Task<Order?> GetByCustomerAsync(Guid customerId, int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<OrderDocument>(
                orderId.ToString(),
                new PartitionKey(customerId.ToString()),
                cancellationToken: cancellationToken);

            return OrderMapper.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId AND c.type = 'Order' ORDER BY c.orderDate DESC")
            .WithParameter("@customerId", customerId.ToString());

        var orders = new List<Order>();
        var iterator = _container.GetItemQueryIterator<OrderDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(customerId.ToString())
        });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            orders.AddRange(response.Select(OrderMapper.ToDomain));
        }

        return orders;
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.status.id = @statusId AND c.type = 'Order' ORDER BY c.orderDate DESC")
            .WithParameter("@statusId", status.Id);

        var orders = new List<Order>();
        var iterator = _container.GetItemQueryIterator<OrderDocument>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            orders.AddRange(response.Select(OrderMapper.ToDomain));
        }

        return orders;
    }

    public async Task DeleteAsync(int orderId, Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<OrderDocument>(
                orderId.ToString(),
                new PartitionKey(customerId.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Item doesn't exist, nothing to delete
        }
    }

    public async Task<bool> ExistsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.id = @id AND c.type = 'Order'")
            .WithParameter("@id", orderId.ToString());

        var iterator = _container.GetItemQueryIterator<int>(query);
        
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            return response.FirstOrDefault() > 0;
        }

        return false;
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'Order' ORDER BY c.orderDate DESC");
        
        var orders = new List<Order>();
        var iterator = _container.GetItemQueryIterator<OrderDocument>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            orders.AddRange(response.Select(OrderMapper.ToDomain));
        }

        return orders;
    }
}
