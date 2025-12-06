namespace CustomerServices.Application.Customers.ChangeStatus;

#region Command

public sealed record ChangeStatusCommand(
    int CustomerId,
    string Action, // Activate, Deactivate, Suspend
    string? Reason) : ICommand;

#endregion

#region Validator

public sealed class ChangeStatusCommandValidator : AbstractValidator<ChangeStatusCommand>
{
    private static readonly string[] ValidActions = { "Activate", "Deactivate", "Suspend" };

    public ChangeStatusCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required")
            .Must(action => ValidActions.Contains(action, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Action must be one of: {string.Join(", ", ValidActions)}");

        When(x => x.Action.Equals("Suspend", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required for suspension");
        });
    }
}

#endregion

#region Handler

public sealed class ChangeStatusCommandHandler : ICommandHandler<ChangeStatusCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerEventStore _eventStore;

    public ChangeStatusCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _eventStore = eventStore;
    }

    public async Task<Result> Handle(
        ChangeStatusCommand request,
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
            switch (request.Action.ToLowerInvariant())
            {
                case "activate":
                    customer.Activate(request.Reason);
                    break;
                case "deactivate":
                    customer.Deactivate(request.Reason);
                    break;
                case "suspend":
                    customer.Suspend(request.Reason!);
                    break;
                default:
                    return Result.Failure($"Unknown action: {request.Action}");
            }

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
