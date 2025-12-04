namespace OrderServices.Infra;

public interface IRequestManager
{
    Task<bool> ExistAsync(Guid id);

    Task CreateRequestForCommandAsync<T>(Guid id);
}

public class RequestManager : IRequestManager
{
    private readonly OrderDbContext _context;

    public RequestManager(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistAsync(Guid id)
    {
        var request = await _context.ClientRequests
            .FindAsync(id);

        return request != null;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid id)
    {
        var exists = await ExistAsync(id);

        if (exists)
        {
            throw new OrderDomainException($"Request with {id} already exists");
        }

        var request = new ClientRequest(id, typeof(T).Name);

        _context.ClientRequests.Add(request);

        await _context.SaveChangesAsync();
    }
}
