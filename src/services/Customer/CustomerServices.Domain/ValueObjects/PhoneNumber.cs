namespace CustomerServices.Domain.ValueObjects;

/// <summary>
/// Value Object representing a phone number
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    public string CountryCode { get; private set; }
    public string Number { get; private set; }

    private PhoneNumber()
    {
        CountryCode = string.Empty;
        Number = string.Empty;
    }

    private PhoneNumber(string countryCode, string number)
    {
        CountryCode = countryCode;
        Number = number;
    }

    public static PhoneNumber Create(string countryCode, string number)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new CustomerDomainException("Country code cannot be empty");

        if (string.IsNullOrWhiteSpace(number))
            throw new CustomerDomainException("Phone number cannot be empty");

        // Remove non-digit characters for validation
        var cleanNumber = new string(number.Where(char.IsDigit).ToArray());

        if (cleanNumber.Length < 7 || cleanNumber.Length > 15)
            throw new CustomerDomainException("Phone number must be between 7 and 15 digits");

        return new PhoneNumber(countryCode.Trim(), cleanNumber);
    }

    public string FullNumber => $"{CountryCode}{Number}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return Number;
    }

    public override string ToString() => $"+{CountryCode} {Number}";
}
