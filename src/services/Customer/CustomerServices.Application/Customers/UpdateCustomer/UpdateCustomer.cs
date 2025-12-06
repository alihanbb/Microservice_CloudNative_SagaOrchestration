namespace CustomerServices.Application.Customers.UpdateCustomer;

#region Command

public sealed record UpdateCustomerCommand(
    int CustomerId,
    string FirstName,
    string LastName,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Street,
    string? City,
    string? State,
    string? Country,
    string? ZipCode,
    int ExpectedVersion) : ICommand;

#endregion

#region Validator

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Expected version must be greater than zero");
    }
}

#endregion

#region Handler

public sealed class UpdateCustomerCommandHandler : ICommandHandler<UpdateCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerEventStore _eventStore;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _eventStore = eventStore;
    }

    public async Task<Result> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure($"Customer with ID {request.CustomerId} was not found");
        }

        // Optimistic concurrency check
        if (customer.Version != request.ExpectedVersion)
        {
            return Result.Failure($"Concurrency conflict. Expected version {request.ExpectedVersion}, but found {customer.Version}");
        }

        var previousVersion = customer.Version;

        try
        {
            // Update name
            customer.UpdateName(request.FirstName, request.LastName);

            // Update phone
            if (!string.IsNullOrWhiteSpace(request.PhoneCountryCode) && !string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                customer.ChangePhone(request.PhoneCountryCode, request.PhoneNumber);
            }
            else
            {
                customer.RemovePhone();
            }

            // Update address
            if (!string.IsNullOrWhiteSpace(request.Street) && 
                !string.IsNullOrWhiteSpace(request.City) &&
                !string.IsNullOrWhiteSpace(request.Country) && 
                !string.IsNullOrWhiteSpace(request.ZipCode))
            {
                customer.ChangeAddress(request.Street, request.City, request.State ?? "", request.Country, request.ZipCode);
            }
            else
            {
                customer.RemoveAddress();
            }

            _customerRepository.Update(customer);
            await _customerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            // Store events
            var events = customer.DomainEvents?.Cast<CustomerDomainEvent>().ToList();
            if (events?.Any() == true)
            {
                await _eventStore.AppendEventsAsync(customer.Id, events, previousVersion, cancellationToken);
            }

            return Result.Success();
        }
        catch (CustomerDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

#endregion
