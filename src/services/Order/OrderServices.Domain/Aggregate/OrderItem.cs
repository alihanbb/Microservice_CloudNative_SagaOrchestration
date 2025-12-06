
using OrderServices.Domain.Exceptions;
using SharedLibrary.SeedWork;

namespace OrderServices.Domain.Aggregate;

public sealed class OrderItem : Entity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Minimum quantity validation
    private const int MinQuantity = 1;
    private const int MaxQuantity = 1000;

    // EF Core için gerekli
    private OrderItem() { }

    public OrderItem(Guid productId, string productName, int quantity, Guid orderId, decimal unitPrice)
    {
        ValidateProductId(productId);
        ValidateProductName(productName);
        ValidateQuantity(quantity);
        ValidateUnitPrice(unitPrice);

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        OrderId = orderId;
        UnitPrice = unitPrice;
    }

    public decimal GetTotalPrice() => UnitPrice * Quantity;

    public void UpdateQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
    }

    public void UpdateUnitPrice(decimal newUnitPrice)
    {
        ValidateUnitPrice(newUnitPrice);
        UnitPrice = newUnitPrice;
    }

    // Private validation methods - Business rules
    private static void ValidateProductId(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new OrderDomainException("Product ID cannot be empty");
    }

    private static void ValidateProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new OrderDomainException("Product name cannot be empty");
        
        if (productName.Length > 200)
            throw new OrderDomainException("Product name cannot exceed 200 characters");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < MinQuantity)
            throw new OrderDomainException($"Quantity must be at least {MinQuantity}");
        
        if (quantity > MaxQuantity)
            throw new OrderDomainException($"Quantity cannot exceed {MaxQuantity}");
    }

    private static void ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
            throw new OrderDomainException("Unit price must be greater than zero");
        
        if (unitPrice > 1_000_000)
            throw new OrderDomainException("Unit price cannot exceed 1,000,000");
    }
}
