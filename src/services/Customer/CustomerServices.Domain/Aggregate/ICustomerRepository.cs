namespace CustomerServices.Domain.Aggregate;

/// <summary>
/// Repository interface for Customer aggregate
/// Follows DDD repository pattern - only aggregate roots have repositories
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>
    /// Adds a new customer
    /// </summary>
    Customer Add(Customer customer);

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    void Update(Customer customer);

    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by email
    /// </summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email is already registered
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers with optional filtering
    /// </summary>
    Task<IEnumerable<Customer>> GetAllAsync(
        CustomerStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of customers with optional status filter
    /// </summary>
    Task<int> GetCountAsync(CustomerStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by name or email
    /// </summary>
    Task<IEnumerable<Customer>> SearchAsync(
        string searchTerm,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
}
