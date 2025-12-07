using Newtonsoft.Json;

namespace OrderServices.Infra.Models;

/// <summary>
/// CosmosDB document model for Order
/// </summary>
public class OrderDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonProperty("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonProperty("orderDate")]
    public DateTime OrderDate { get; set; }

    [JsonProperty("status")]
    public OrderStatusDocument Status { get; set; } = new();

    [JsonProperty("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonProperty("orderItems")]
    public List<OrderItemDocument> OrderItems { get; set; } = new();

    [JsonProperty("_etag")]
    public string? ETag { get; set; }

    // Partition key
    [JsonProperty("type")]
    public string Type { get; set; } = "Order";
}

public class OrderStatusDocument
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

public class OrderItemDocument
{
    [JsonProperty("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonProperty("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("unitPrice")]
    public decimal UnitPrice { get; set; }
}
