namespace CustomerServices.Application.Customers.DeleteCustomer;

#region Command

public sealed record DeleteCustomerCommand(
    int CustomerId,
    string Reason) : ICommand;

#endregion

#region Validator

public sealed class DeleteCustomerCommandValidator : AbstractValidator<DeleteCustomerCommand>
{
    public DeleteCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Deletion reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}

#endregion

#region Handler

public sealed class DeleteCustomerCommandHandler : ICommandHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerEventStore _eventStore;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _eventStore = eventStore;
    }

    public async Task<Result> Handle(
        DeleteCustomerCommand request,
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
            customer.Delete(request.Reason);

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
