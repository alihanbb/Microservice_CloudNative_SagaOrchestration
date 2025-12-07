using CustomerServices.Domain.ValueObjects;

namespace CustomerServices.Domain.Aggregate;

public sealed class Customer : Entity, IAggregateRoot
{
    
    public CustomerName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public PhoneNumber? Phone { get; private set; }
    public Address? Address { get; private set; }
    public CustomerStatus Status { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    
    public int Version { get; private set; }

    private Customer() { }

    private Customer(string firstName, string lastName, string email)
    {
        Name = CustomerName.Create(firstName, lastName);
        Email = ValueObjects.Email.Create(email);
        Status = CustomerStatus.PendingVerification;
        CreatedAt = DateTime.UtcNow;
        Version = 1;

        AddDomainEvent(new CustomerCreatedDomainEvent(
            Id,
            firstName,
            lastName,
            email,
            Version));
    }

    public static Customer Create(string firstName, string lastName, string email)
    {
        return new Customer(firstName, lastName, email);
    }

    public static Customer CreateWithDetails(
        string firstName,
        string lastName,
        string email,
        string? countryCode,
        string? phoneNumber,
        string? street,
        string? city,
        string? state,
        string? country,
        string? zipCode)
    {
        var customer = new Customer(firstName, lastName, email);

        if (!string.IsNullOrWhiteSpace(countryCode) && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            customer.ChangePhone(countryCode, phoneNumber);
        }

        if (!string.IsNullOrWhiteSpace(street) && !string.IsNullOrWhiteSpace(city) &&
            !string.IsNullOrWhiteSpace(country) && !string.IsNullOrWhiteSpace(zipCode))
        {
            customer.ChangeAddress(street, city, state ?? string.Empty, country, zipCode);
        }

        return customer;
    }

    public void UpdateName(string firstName, string lastName)
    {
        EnsureNotDeleted();

        var oldFirstName = Name.FirstName;
        var oldLastName = Name.LastName;

        Name = CustomerName.Create(firstName, lastName);
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerNameUpdatedDomainEvent(
            Id,
            oldFirstName,
            oldLastName,
            firstName,
            lastName,
            Version));
    }

    public void ChangeEmail(string newEmail)
    {
        EnsureNotDeleted();

        var oldEmail = Email.Value;
        var newEmailVO = ValueObjects.Email.Create(newEmail);

        if (Email == newEmailVO)
            return; 

        Email = newEmailVO;
        IncrementVersion();
        UpdateTimestamp();

        if (Status == CustomerStatus.Active)
        {
            Status = CustomerStatus.PendingVerification;
            VerifiedAt = null;
        }

        AddDomainEvent(new CustomerEmailChangedDomainEvent(
            Id,
            oldEmail,
            newEmail,
            Version));
    }

    public void ChangePhone(string countryCode, string phoneNumber)
    {
        EnsureNotDeleted();

        Phone = PhoneNumber.Create(countryCode, phoneNumber);
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerPhoneChangedDomainEvent(
            Id,
            countryCode,
            phoneNumber,
            Version));
    }

    public void RemovePhone()
    {
        EnsureNotDeleted();

        if (Phone == null)
            return;

        Phone = null;
        IncrementVersion();
        UpdateTimestamp();
    }

    public void ChangeAddress(string street, string city, string state, string country, string zipCode)
    {
        EnsureNotDeleted();

        Address = ValueObjects.Address.Create(street, city, state, country, zipCode);
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerAddressChangedDomainEvent(
            Id,
            street,
            city,
            state,
            country,
            zipCode,
            Version));
    }

    public void RemoveAddress()
    {
        EnsureNotDeleted();

        if (Address == null)
            return;

        Address = null;
        IncrementVersion();
        UpdateTimestamp();
    }

    public void Verify()
    {
        EnsureNotDeleted();

        if (Status != CustomerStatus.PendingVerification)
            throw new CustomerDomainException($"Cannot verify customer in {Status.Name} status");

        var oldStatus = Status.Name;
        Status = CustomerStatus.Active;
        VerifiedAt = DateTime.UtcNow;
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerVerifiedDomainEvent(Id, Version));
        AddDomainEvent(new CustomerStatusChangedDomainEvent(
            Id, oldStatus, Status.Name, "Email verified", Version));
    }

    public void Activate(string? reason = null)
    {
        EnsureNotDeleted();

        if (Status == CustomerStatus.Active)
            return;

        if (Status == CustomerStatus.PendingVerification)
            throw new CustomerDomainException("Customer must be verified before activation");

        var oldStatus = Status.Name;
        Status = CustomerStatus.Active;
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerStatusChangedDomainEvent(
            Id, oldStatus, Status.Name, reason, Version));
    }

    public void Deactivate(string? reason = null)
    {
        EnsureNotDeleted();

        if (Status == CustomerStatus.Inactive)
            return;

        var oldStatus = Status.Name;
        Status = CustomerStatus.Inactive;
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerStatusChangedDomainEvent(
            Id, oldStatus, Status.Name, reason, Version));
    }

    public void Suspend(string reason)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(reason))
            throw new CustomerDomainException("Suspension reason is required");

        if (Status == CustomerStatus.Suspended)
            return;

        var oldStatus = Status.Name;
        Status = CustomerStatus.Suspended;
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerStatusChangedDomainEvent(
            Id, oldStatus, Status.Name, reason, Version));
    }

    public void Delete(string reason)
    {
        if (Status == CustomerStatus.Deleted)
            return;

        if (string.IsNullOrWhiteSpace(reason))
            throw new CustomerDomainException("Deletion reason is required");

        var oldStatus = Status.Name;
        Status = CustomerStatus.Deleted;
        DeletedAt = DateTime.UtcNow;
        IncrementVersion();
        UpdateTimestamp();

        AddDomainEvent(new CustomerDeletedDomainEvent(Id, reason, Version));
        AddDomainEvent(new CustomerStatusChangedDomainEvent(
            Id, oldStatus, Status.Name, reason, Version));
    }


    public bool IsActive() => Status == CustomerStatus.Active;
    public bool IsVerified() => VerifiedAt.HasValue;
    public bool IsDeleted() => Status == CustomerStatus.Deleted;
    public bool IsSuspended() => Status == CustomerStatus.Suspended;
    public bool CanBeModified() => !IsDeleted();

    public void Apply(CustomerDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreatedDomainEvent e:
                Apply(e);
                break;
            case CustomerNameUpdatedDomainEvent e:
                Apply(e);
                break;
            case CustomerEmailChangedDomainEvent e:
                Apply(e);
                break;
            case CustomerPhoneChangedDomainEvent e:
                Apply(e);
                break;
            case CustomerAddressChangedDomainEvent e:
                Apply(e);
                break;
            case CustomerStatusChangedDomainEvent e:
                Apply(e);
                break;
            case CustomerVerifiedDomainEvent e:
                Apply(e);
                break;
            case CustomerDeletedDomainEvent e:
                Apply(e);
                break;
            default:
                throw new CustomerDomainException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(CustomerCreatedDomainEvent e)
    {
        Name = CustomerName.Create(e.FirstName, e.LastName);
        Email = ValueObjects.Email.Create(e.Email);
        Status = CustomerStatus.PendingVerification;
        CreatedAt = e.OccurredOn;
        Version = e.Version;
    }

    private void Apply(CustomerNameUpdatedDomainEvent e)
    {
        Name = CustomerName.Create(e.NewFirstName, e.NewLastName);
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    private void Apply(CustomerEmailChangedDomainEvent e)
    {
        Email = ValueObjects.Email.Create(e.NewEmail);
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    private void Apply(CustomerPhoneChangedDomainEvent e)
    {
        Phone = PhoneNumber.Create(e.CountryCode, e.PhoneNumber);
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    private void Apply(CustomerAddressChangedDomainEvent e)
    {
        Address = ValueObjects.Address.Create(e.Street, e.City, e.State, e.Country, e.ZipCode);
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    private void Apply(CustomerStatusChangedDomainEvent e)
    {
        Status = CustomerStatus.FromName(e.NewStatus);
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    private void Apply(CustomerVerifiedDomainEvent e)
    {
        VerifiedAt = e.VerifiedAt;
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    private void Apply(CustomerDeletedDomainEvent e)
    {
        DeletedAt = e.DeletedAt;
        Version = e.Version;
        UpdatedAt = e.OccurredOn;
    }

    /// <summary>
    /// Rebuilds customer state from event history
    /// </summary>
    public static Customer FromHistory(IEnumerable<CustomerDomainEvent> events)
    {
        var customer = new Customer();
        
        foreach (var @event in events.OrderBy(e => e.Version))
        {
            customer.Apply(@event);
        }

        return customer;
    }

    // ============================================
    // PRIVATE HELPERS
    // ============================================

    private void EnsureNotDeleted()
    {
        if (IsDeleted())
            throw new CustomerDomainException("Cannot modify a deleted customer");
    }

    private void IncrementVersion()
    {
        Version++;
    }

    private void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
