using OrderServices.Domain.SeedWork;

namespace OrderServices.Domain.Aggregate;

public class OrderStatus : ValueObject
{
    public static OrderStatus Pending = new(1, nameof(Pending));
    public static OrderStatus Confirmed = new(2, nameof(Confirmed));
    public static OrderStatus Paid = new(3, nameof(Paid));
    public static OrderStatus Shipped = new(4, nameof(Shipped));
    public static OrderStatus Delivered = new(5, nameof(Delivered));
    public static OrderStatus Cancelled = new(6, nameof(Cancelled));
    public static OrderStatus Refunded = new(7, nameof(Refunded));

    public int Id { get; private set; }
    public string Name { get; private set; }

    protected OrderStatus() { }

    private OrderStatus(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public static IEnumerable<OrderStatus> GetAll()
    {
        yield return Pending;
        yield return Confirmed;
        yield return Paid;
        yield return Shipped;
        yield return Delivered;
        yield return Cancelled;
        yield return Refunded;
    }

    public static OrderStatus FromId(int id)
    {
        return GetAll().FirstOrDefault(s => s.Id == id)
            ?? throw new ArgumentException($"Invalid OrderStatus id: {id}", nameof(id));
    }

    public static OrderStatus FromName(string name)
    {
        return GetAll().FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Invalid OrderStatus name: {name}", nameof(name));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }

    public override string ToString() => Name;
}

