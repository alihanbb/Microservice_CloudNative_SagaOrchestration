namespace CustomerServices.Infra.Repositories;

/// <summary>
/// Repository implementation for Customer aggregate
/// Provides data access with Unit of Work pattern
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _context;
    private readonly IMediator _mediator;

    public IUnitOfWork UnitOfWork => new UnitOfWorkAdapter(_context, _mediator);

    public CustomerRepository(CustomerDbContext context, IMediator mediator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Adds a new customer
    /// </summary>
    public Customer Add(Customer customer)
    {
        return _context.Customers.Add(customer).Entity;
    }

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    public void Update(Customer customer)
    {
        _context.Entry(customer).State = EntityState.Modified;
    }

    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }

    /// <summary>
    /// Gets a customer by email
    /// </summary>
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.Value == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Checks if email is already registered
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        return await _context.Customers
            .AnyAsync(c => c.Email.Value == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Gets all customers with optional filtering
    /// </summary>
    public async Task<IEnumerable<Customer>> GetAllAsync(
        CustomerStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsQueryable();

        if (status != null)
        {
            query = query.Where(c => c.Status.Id == status.Id);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets total count of customers
    /// </summary>
    public async Task<int> GetCountAsync(CustomerStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsQueryable();

        if (status != null)
        {
            query = query.Where(c => c.Status.Id == status.Id);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Searches customers by name or email
    /// </summary>
    public async Task<IEnumerable<Customer>> SearchAsync(
        string searchTerm,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync(skip: skip, take: take, cancellationToken: cancellationToken);
        }

        var term = searchTerm.Trim().ToLowerInvariant();

        return await _context.Customers
            .Where(c => 
                c.Name.FirstName.ToLower().Contains(term) ||
                c.Name.LastName.ToLower().Contains(term) ||
                c.Email.Value.Contains(term))
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets customer including deleted ones (bypasses query filter)
    /// </summary>
    public async Task<Customer?> GetByIdIncludingDeletedAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }
}

/// <summary>
/// Adapter to provide UnitOfWork with MediatR support
/// </summary>
internal class UnitOfWorkAdapter : IUnitOfWork
{
    private readonly CustomerDbContext _context;
    private readonly IMediator _mediator;

    public UnitOfWorkAdapter(CustomerDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveEntitiesAsync(_mediator, cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
