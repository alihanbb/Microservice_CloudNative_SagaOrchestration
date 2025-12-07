using System.Text.Json;

namespace CustomerServices.Infra.EventSourcing;

public class CustomerSnapshot
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int Version { get; set; }
    public string SnapshotData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    private CustomerSnapshot() { }

    public static CustomerSnapshot FromCustomer(Customer customer)
    {
        var snapshotData = new CustomerSnapshotData
        {
            Id = customer.Id,
            FirstName = customer.Name.FirstName,
            LastName = customer.Name.LastName,
            Email = customer.Email.Value,
            PhoneCountryCode = customer.Phone?.CountryCode,
            PhoneNumber = customer.Phone?.Number,
            Street = customer.Address?.Street,
            City = customer.Address?.City,
            State = customer.Address?.State,
            Country = customer.Address?.Country,
            ZipCode = customer.Address?.ZipCode,
            StatusId = customer.Status.Id,
            StatusName = customer.Status.Name,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            VerifiedAt = customer.VerifiedAt,
            DeletedAt = customer.DeletedAt,
            Version = customer.Version
        };

        return new CustomerSnapshot
        {
            CustomerId = customer.Id,
            Version = customer.Version,
            SnapshotData = JsonSerializer.Serialize(snapshotData),
            CreatedAt = DateTime.UtcNow
        };
    }

    public CustomerSnapshotData? ToSnapshotData()
    {
        return JsonSerializer.Deserialize<CustomerSnapshotData>(SnapshotData);
    }
}

public class CustomerSnapshotData
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneCountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; }
}
