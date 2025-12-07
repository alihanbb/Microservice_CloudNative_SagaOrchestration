namespace CustomerServices.Domain.Aggregate;

public sealed class CustomerStatus : ValueObject
{
    public static readonly CustomerStatus Active = new(1, nameof(Active));
    public static readonly CustomerStatus Inactive = new(2, nameof(Inactive));
    public static readonly CustomerStatus Suspended = new(3, nameof(Suspended));
    public static readonly CustomerStatus Deleted = new(4, nameof(Deleted));
    public static readonly CustomerStatus PendingVerification = new(5, nameof(PendingVerification));

    public int Id { get; private set; }
    public string Name { get; private set; }

    private CustomerStatus()
    {
        Id = 0;
        Name = string.Empty;
    }

    private CustomerStatus(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public static IEnumerable<CustomerStatus> GetAll()
    {
        yield return Active;
        yield return Inactive;
        yield return Suspended;
        yield return Deleted;
        yield return PendingVerification;
    }

    public static CustomerStatus FromId(int id)
    {
        return GetAll().FirstOrDefault(s => s.Id == id)
            ?? throw new CustomerDomainException($"Invalid CustomerStatus id: {id}");
    }

    public static CustomerStatus FromName(string name)
    {
        return GetAll().FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new CustomerDomainException($"Invalid CustomerStatus name: {name}");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }

    public override string ToString() => Name;
}
