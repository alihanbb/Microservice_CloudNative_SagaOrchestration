namespace CustomerServices.Domain.Aggregate;

public interface ICustomerRepository : IRepository<Customer>
{
    Customer Add(Customer customer);

    void Update(Customer customer);

    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IEnumerable<Customer>> GetAllAsync(
        CustomerStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CustomerStatus? status = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Customer>> SearchAsync(
        string searchTerm,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
}
