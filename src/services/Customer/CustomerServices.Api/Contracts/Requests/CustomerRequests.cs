namespace CustomerServices.Api.Contracts.Requests;

public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Street,
    string? City,
    string? State,
    string? Country,
    string? ZipCode);

public sealed record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Street,
    string? City,
    string? State,
    string? Country,
    string? ZipCode,
    int ExpectedVersion);

public sealed record ChangeEmailRequest(
    string NewEmail,
    int ExpectedVersion);

public sealed record ChangeStatusRequest(
    string Action,
    string? Reason);

public sealed record DeleteCustomerRequest(string Reason);
