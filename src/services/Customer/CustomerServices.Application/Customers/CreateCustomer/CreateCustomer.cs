namespace CustomerServices.Application.Customers.CreateCustomer;

#region Command

/// <summary>
/// Command to create a new customer
/// </summary>
public sealed record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Street,
    string? City,
    string? State,
    string? Country,
    string? ZipCode) : ICommand<CreateCustomerResponse>;

#endregion

#region Response

public sealed record CreateCustomerResponse(
    int CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    DateTime CreatedAt,
    int Version);

#endregion

#region Validator

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerCommandValidator(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
            .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneCountryCode)
                .NotEmpty().WithMessage("Country code is required when phone number is provided");
        });
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _customerRepository.ExistsByEmailAsync(email, cancellationToken);
    }
}

#endregion

#region Handler

public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, CreateCustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerEventStore _eventStore;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _eventStore = eventStore;
    }

    public async Task<Result<CreateCustomerResponse>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        // Create customer using factory method
        var customer = Customer.CreateWithDetails(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneCountryCode,
            request.PhoneNumber,
            request.Street,
            request.City,
            request.State,
            request.Country,
            request.ZipCode);

        // Add to repository
        _customerRepository.Add(customer);
        await _customerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Store events for Event Sourcing
        var events = customer.DomainEvents?.Cast<CustomerDomainEvent>().ToList();
        if (events?.Any() == true)
        {
            await _eventStore.AppendEventsAsync(customer.Id, events, 0, cancellationToken);
        }

        return Result<CreateCustomerResponse>.Success(new CreateCustomerResponse(
            customer.Id,
            customer.Name.FirstName,
            customer.Name.LastName,
            customer.Email.Value,
            customer.Status.Name,
            customer.CreatedAt,
            customer.Version));
    }
}

#endregion
