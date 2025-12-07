using System.Reflection;
using OrderServices.Infra.Models;

namespace OrderServices.Infra.Mappers;

/// <summary>
/// Maps between domain Order entities and CosmosDB OrderDocument
/// </summary>
public static class OrderMapper
{
    public static OrderDocument ToDocument(Order order)
    {
        return new OrderDocument
        {
            Id = order.Id.ToString(),
            CustomerId = order.CustomerId.ToString(),
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            Status = new OrderStatusDocument
            {
                Id = order.Status.Id,
                Name = order.Status.Name
            },
            TotalAmount = order.TotalAmount,
            OrderItems = order.OrderItems.Select(item => new OrderItemDocument
            {
                ProductId = item.ProductId.ToString(),
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            Type = "Order"
        };
    }

    public static Order ToDomain(OrderDocument document)
    {
        // Create order using parameterless constructor via reflection
        var order = (Order)Activator.CreateInstance(typeof(Order), true)!;
        
        // Set Id via Entity base class backing field
        SetFieldValue(order, "_id", int.Parse(document.Id));
        
        // Set Order properties via backing fields
        SetBackingField(order, "CustomerId", Guid.Parse(document.CustomerId));
        SetBackingField(order, "CustomerName", document.CustomerName);
        SetBackingField(order, "OrderDate", document.OrderDate);
        SetBackingField(order, "Status", OrderStatus.FromId(document.Status.Id));
        SetBackingField(order, "TotalAmount", document.TotalAmount);

        // Set order items using the private field directly
        var orderItemsField = typeof(Order).GetField("_orderItems", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var orderItems = new List<OrderItem>();
        foreach (var itemDoc in document.OrderItems)
        {
            var orderItem = (OrderItem)Activator.CreateInstance(typeof(OrderItem), true)!;
            
            // OrderItem also has backing field for Id in Entity
            SetFieldValue(orderItem, "_id", 0); // Default ID for items
            SetBackingField(orderItem, "ProductId", Guid.Parse(itemDoc.ProductId));
            SetBackingField(orderItem, "ProductName", itemDoc.ProductName);
            SetBackingField(orderItem, "Quantity", itemDoc.Quantity);
            SetBackingField(orderItem, "UnitPrice", itemDoc.UnitPrice);
            SetBackingField(orderItem, "OrderId", Guid.Empty);
            
            orderItems.Add(orderItem);
        }
        
        orderItemsField?.SetValue(order, orderItems);

        return order;
    }

    private static void SetBackingField<T>(object obj, string propertyName, T value)
    {
        // Try auto-property backing field first
        var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }

        // Try property in base types
        var type = obj.GetType();
        while (type != null)
        {
            field = type.GetField($"<{propertyName}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            type = type.BaseType;
        }
    }

    private static void SetFieldValue<T>(object obj, string fieldName, T value)
    {
        var type = obj.GetType();
        FieldInfo? field = null;
        
        // Search through type hierarchy
        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }
        
        field?.SetValue(obj, value);
    }
}
