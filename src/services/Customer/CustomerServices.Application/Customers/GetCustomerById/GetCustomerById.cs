namespace CustomerServices.Application.Customers.GetCustomerById;

#region Query

public sealed record GetCustomerByIdQuery(int CustomerId) : IQuery<CustomerDetailResponse>;

#endregion

#region Response

public sealed record CustomerDetailResponse(
    int CustomerId,
    string FirstName,
    string LastName,
    string Email,
    PhoneResponse? Phone,
    AddressResponse? Address,
    string Status,
    bool IsVerified,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? VerifiedAt,
    int Version);

public sealed record PhoneResponse(string CountryCode, string Number, string FullNumber);
public sealed record AddressResponse(string Street, string City, string State, string Country, string ZipCode);

#endregion

#region Validator

public sealed class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");
    }
}

#endregion

#region Handler

public sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDetailResponse>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerDetailResponse>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result<CustomerDetailResponse>.Failure($"Customer with ID {request.CustomerId} was not found");
        }

        var response = new CustomerDetailResponse(
            customer.Id,
            customer.Name.FirstName,
            customer.Name.LastName,
            customer.Email.Value,
            customer.Phone != null 
                ? new PhoneResponse(customer.Phone.CountryCode, customer.Phone.Number, customer.Phone.FullNumber) 
                : null,
            customer.Address != null 
                ? new AddressResponse(
                    customer.Address.Street, 
                    customer.Address.City, 
                    customer.Address.State, 
                    customer.Address.Country, 
                    customer.Address.ZipCode) 
                : null,
            customer.Status.Name,
            customer.IsVerified(),
            customer.CreatedAt,
            customer.UpdatedAt,
            customer.VerifiedAt,
            customer.Version);

        return Result<CustomerDetailResponse>.Success(response);
    }
}

#endregion
