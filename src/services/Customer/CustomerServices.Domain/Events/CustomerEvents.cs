 namespace CustomerServices.Domain.Events;

public sealed record CustomerCreatedDomainEvent : CustomerDomainEvent
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }

    public CustomerCreatedDomainEvent(
        int customerId,
        string firstName,
        string lastName,
        string email,
        int version)
    {
        CustomerId = customerId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer email is changed
/// </summary>
public sealed record CustomerEmailChangedDomainEvent : CustomerDomainEvent
{
    public string OldEmail { get; init; }
    public string NewEmail { get; init; }

    public CustomerEmailChangedDomainEvent(
        int customerId,
        string oldEmail,
        string newEmail,
        int version)
    {
        CustomerId = customerId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer name is updated
/// </summary>
public sealed record CustomerNameUpdatedDomainEvent : CustomerDomainEvent
{
    public string OldFirstName { get; init; }
    public string OldLastName { get; init; }
    public string NewFirstName { get; init; }
    public string NewLastName { get; init; }

    public CustomerNameUpdatedDomainEvent(
        int customerId,
        string oldFirstName,
        string oldLastName,
        string newFirstName,
        string newLastName,
        int version)
    {
        CustomerId = customerId;
        OldFirstName = oldFirstName;
        OldLastName = oldLastName;
        NewFirstName = newFirstName;
        NewLastName = newLastName;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer address is changed
/// </summary>
public sealed record CustomerAddressChangedDomainEvent : CustomerDomainEvent
{
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string Country { get; init; }
    public string ZipCode { get; init; }

    public CustomerAddressChangedDomainEvent(
        int customerId,
        string street,
        string city,
        string state,
        string country,
        string zipCode,
        int version)
    {
        CustomerId = customerId;
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer phone number is changed
/// </summary>
public sealed record CustomerPhoneChangedDomainEvent : CustomerDomainEvent
{
    public string CountryCode { get; init; }
    public string PhoneNumber { get; init; }

    public CustomerPhoneChangedDomainEvent(
        int customerId,
        string countryCode,
        string phoneNumber,
        int version)
    {
        CustomerId = customerId;
        CountryCode = countryCode;
        PhoneNumber = phoneNumber;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer status changes
/// </summary>
public sealed record CustomerStatusChangedDomainEvent : CustomerDomainEvent
{
    public string OldStatus { get; init; }
    public string NewStatus { get; init; }
    public string? Reason { get; init; }

    public CustomerStatusChangedDomainEvent(
        int customerId,
        string oldStatus,
        string newStatus,
        string? reason,
        int version)
    {
        CustomerId = customerId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer is verified
/// </summary>
public sealed record CustomerVerifiedDomainEvent : CustomerDomainEvent
{
    public DateTime VerifiedAt { get; init; }

    public CustomerVerifiedDomainEvent(int customerId, int version)
    {
        CustomerId = customerId;
        VerifiedAt = DateTime.UtcNow;
        Version = version;
    }
}

/// <summary>
/// Event raised when customer is deleted (soft delete)
/// </summary>
public sealed record CustomerDeletedDomainEvent : CustomerDomainEvent
{
    public string Reason { get; init; }
    public DateTime DeletedAt { get; init; }

    public CustomerDeletedDomainEvent(int customerId, string reason, int version)
    {
        CustomerId = customerId;
        Reason = reason;
        DeletedAt = DateTime.UtcNow;
        Version = version;
    }
}
