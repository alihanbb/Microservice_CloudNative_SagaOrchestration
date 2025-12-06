namespace CustomerServices.Domain.ValueObjects;

/// <summary>
/// Value Object representing customer name
/// </summary>
public sealed class CustomerName : ValueObject
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    private CustomerName()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    private CustomerName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static CustomerName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new CustomerDomainException("First name cannot be empty");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new CustomerDomainException("Last name cannot be empty");

        if (firstName.Length > 100)
            throw new CustomerDomainException("First name cannot exceed 100 characters");

        if (lastName.Length > 100)
            throw new CustomerDomainException("Last name cannot exceed 100 characters");

        return new CustomerName(firstName.Trim(), lastName.Trim());
    }

    public string FullName => $"{FirstName} {LastName}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString() => FullName;
}
