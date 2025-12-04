namespace OrderServices.Infra;

public class ClientRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Time { get; set; }

    public ClientRequest() { }

    public ClientRequest(Guid id, string name)
    {
        Id = id;
        Name = !string.IsNullOrWhiteSpace(name) 
            ? name 
            : throw new ArgumentNullException(nameof(name));
        Time = DateTime.UtcNow;
    }
}
