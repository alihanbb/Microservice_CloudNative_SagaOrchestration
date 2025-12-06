namespace CustomerServices.Application.Customers.VerifyCustomer;

#region Command

public sealed record VerifyCustomerCommand(int CustomerId) : ICommand;

#endregion

#region Validator

public sealed class VerifyCustomerCommandValidator : AbstractValidator<VerifyCustomerCommand>
{
    public VerifyCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");
    }
}

#endregion

#region Handler

public sealed class VerifyCustomerCommandHandler : ICommandHandler<VerifyCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerEventStore _eventStore;

    public VerifyCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _eventStore = eventStore;
    }

    public async Task<Result> Handle(
        VerifyCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure($"Customer with ID {request.CustomerId} was not found");
        }

        var previousVersion = customer.Version;

        try
        {
            customer.Verify();

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
