namespace CustomerServices.Application.Customers.ChangeEmail;

#region Command

public sealed record ChangeEmailCommand(
    int CustomerId,
    string NewEmail,
    int ExpectedVersion) : ICommand;

#endregion

#region Validator

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    private readonly ICustomerRepository _customerRepository;

    public ChangeEmailCommandValidator(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");

        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Expected version must be greater than zero");
    }

    private async Task<bool> BeUniqueEmail(ChangeEmailCommand command, string email, CancellationToken cancellationToken)
    {
        var existingCustomer = await _customerRepository.GetByEmailAsync(email, cancellationToken);
        return existingCustomer == null || existingCustomer.Id == command.CustomerId;
    }
}

#endregion

#region Handler

public sealed class ChangeEmailCommandHandler : ICommandHandler<ChangeEmailCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerEventStore _eventStore;

    public ChangeEmailCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _eventStore = eventStore;
    }

    public async Task<Result> Handle(
        ChangeEmailCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure($"Customer with ID {request.CustomerId} was not found");
        }

        if (customer.Version != request.ExpectedVersion)
        {
            return Result.Failure($"Concurrency conflict. Expected version {request.ExpectedVersion}, but found {customer.Version}");
        }

        var previousVersion = customer.Version;

        try
        {
            customer.ChangeEmail(request.NewEmail);

            _customerRepository.Update(customer);
            await _customerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

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
