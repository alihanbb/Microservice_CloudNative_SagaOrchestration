namespace CustomerServices.Infra;

/// <summary>
/// Customer DbContext implementing Unit of Work pattern
/// Handles persistence and domain event dispatching
/// Compatible with DbContext Pooling (single constructor)
/// </summary>
public class CustomerDbContext : DbContext, IUnitOfWork
{
    public const string DEFAULT_SCHEMA = "customer";

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerEventEntity> CustomerEvents { get; set; } = null!;
    public DbSet<CustomerSnapshot> CustomerSnapshots { get; set; } = null!;

    private IDbContextTransaction? _currentTransaction;

    public IDbContextTransaction? GetCurrentTransaction() => _currentTransaction;
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// Single constructor for DbContext Pooling compatibility
    /// </summary>
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerEventEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerSnapshotEntityTypeConfiguration());
    }

    /// <summary>
    /// Saves all changes and dispatches domain events
    /// MediatR is resolved from the service provider when needed
    /// </summary>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Save changes to database
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Saves all changes, dispatches domain events using provided mediator
    /// </summary>
    public async Task<bool> SaveEntitiesAsync(IMediator mediator, CancellationToken cancellationToken = default)
    {
        // Dispatch Domain Events before saving
        await mediator.DispatchDomainEventsAsync(this);

        // Save changes to database
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    public async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return null;

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);

        return _currentTransaction;
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction != _currentTransaction)
            throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");

        try
        {
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}
