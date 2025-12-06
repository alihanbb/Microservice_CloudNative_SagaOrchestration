namespace CustomerServices.Application.Customers.GetCustomerHistory;

#region Query

/// <summary>
/// Query to get customer event history (Event Sourcing)
/// </summary>
public sealed record GetCustomerHistoryQuery(int CustomerId) : IQuery<CustomerHistoryResponse>;

#endregion

#region Response

public sealed record CustomerHistoryResponse(
    int CustomerId,
    int CurrentVersion,
    List<CustomerEventResponse> Events);

public sealed record CustomerEventResponse(
    Guid EventId,
    string EventType,
    int Version,
    DateTime OccurredOn,
    Dictionary<string, object?> Data);

#endregion

#region Validator

public sealed class GetCustomerHistoryQueryValidator : AbstractValidator<GetCustomerHistoryQuery>
{
    public GetCustomerHistoryQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be greater than zero");
    }
}

#endregion

#region Handler

public sealed class GetCustomerHistoryQueryHandler : IQueryHandler<GetCustomerHistoryQuery, CustomerHistoryResponse>
{
    private readonly ICustomerEventStore _eventStore;

    public GetCustomerHistoryQueryHandler(ICustomerEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result<CustomerHistoryResponse>> Handle(
        GetCustomerHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var events = await _eventStore.GetEventsAsync(request.CustomerId, cancellationToken);
        var eventList = events.ToList();

        if (!eventList.Any())
        {
            return Result<CustomerHistoryResponse>.Failure($"No history found for customer {request.CustomerId}");
        }

        var currentVersion = eventList.Max(e => e.Version);

        var eventResponses = eventList.Select(e => new CustomerEventResponse(
            e.EventId,
            e.GetType().Name.Replace("DomainEvent", ""),
            e.Version,
            e.OccurredOn,
            GetEventData(e))).ToList();

        return Result<CustomerHistoryResponse>.Success(new CustomerHistoryResponse(
            request.CustomerId,
            currentVersion,
            eventResponses));
    }

    private static Dictionary<string, object?> GetEventData(CustomerDomainEvent @event)
    {
        var data = new Dictionary<string, object?>();

        switch (@event)
        {
            case CustomerCreatedDomainEvent e:
                data["FirstName"] = e.FirstName;
                data["LastName"] = e.LastName;
                data["Email"] = e.Email;
                break;

            case CustomerNameUpdatedDomainEvent e:
                data["OldFirstName"] = e.OldFirstName;
                data["OldLastName"] = e.OldLastName;
                data["NewFirstName"] = e.NewFirstName;
                data["NewLastName"] = e.NewLastName;
                break;

            case CustomerEmailChangedDomainEvent e:
                data["OldEmail"] = e.OldEmail;
                data["NewEmail"] = e.NewEmail;
                break;

            case CustomerPhoneChangedDomainEvent e:
                data["CountryCode"] = e.CountryCode;
                data["PhoneNumber"] = e.PhoneNumber;
                break;

            case CustomerAddressChangedDomainEvent e:
                data["Street"] = e.Street;
                data["City"] = e.City;
                data["State"] = e.State;
                data["Country"] = e.Country;
                data["ZipCode"] = e.ZipCode;
                break;

            case CustomerStatusChangedDomainEvent e:
                data["OldStatus"] = e.OldStatus;
                data["NewStatus"] = e.NewStatus;
                data["Reason"] = e.Reason;
                break;

            case CustomerVerifiedDomainEvent e:
                data["VerifiedAt"] = e.VerifiedAt;
                break;

            case CustomerDeletedDomainEvent e:
                data["Reason"] = e.Reason;
                data["DeletedAt"] = e.DeletedAt;
                break;
        }

        return data;
    }
}

#endregion
