namespace CustomerServices.Domain.ValueObjects;

/// <summary>
/// Value Object representing an email address
/// Encapsulates email validation and formatting
/// </summary>
public sealed class Email : ValueObject
{
    public string Value { get; private set; }

    private Email() => Value = string.Empty;

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new CustomerDomainException("Email cannot be empty");

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 256)
            throw new CustomerDomainException("Email cannot exceed 256 characters");

        if (!IsValidEmail(email))
            throw new CustomerDomainException("Invalid email format");

        return new Email(email);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
