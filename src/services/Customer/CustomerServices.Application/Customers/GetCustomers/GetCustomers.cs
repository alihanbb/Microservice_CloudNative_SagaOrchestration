namespace CustomerServices.Application.Customers.GetCustomers;

#region Query

public sealed record GetCustomersQuery(
    string? Status,
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedCustomerResponse>;

#endregion

#region Response

public sealed record PagedCustomerResponse(
    List<CustomerSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record CustomerSummaryResponse(
    int CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    bool IsVerified,
    DateTime CreatedAt);

#endregion

#region Validator

public sealed class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}

#endregion

#region Handler

public sealed class GetCustomersQueryHandler : IQueryHandler<GetCustomersQuery, PagedCustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<PagedCustomerResponse>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;
        
        CustomerStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            try
            {
                status = CustomerStatus.FromName(request.Status);
            }
            catch
            {
                return Result<PagedCustomerResponse>.Failure($"Invalid status: {request.Status}");
            }
        }

        IEnumerable<Customer> customers;
        int totalCount;

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            customers = await _customerRepository.SearchAsync(
                request.SearchTerm, skip, request.PageSize, cancellationToken);
            // For search, we need to get total count separately
            totalCount = (await _customerRepository.SearchAsync(
                request.SearchTerm, 0, int.MaxValue, cancellationToken)).Count();
        }
        else
        {
            customers = await _customerRepository.GetAllAsync(
                status, skip, request.PageSize, cancellationToken);
            totalCount = await _customerRepository.GetCountAsync(status, cancellationToken);
        }

        var items = customers.Select(c => new CustomerSummaryResponse(
            c.Id,
            c.Name.FirstName,
            c.Name.LastName,
            c.Email.Value,
            c.Status.Name,
            c.IsVerified(),
            c.CreatedAt)).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return Result<PagedCustomerResponse>.Success(new PagedCustomerResponse(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }
}

#endregion
