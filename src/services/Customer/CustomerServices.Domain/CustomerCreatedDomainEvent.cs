namespace CustomerServices.Domain
{
    public sealed record CustomerCreatedDomainEvent
    {
        public Guid CustomerId { get; init; }
        public string Email { get; init; } = string.Empty;
        public DateTime OncurredOn { get; init; } = DateTime.UtcNow;
    }
}
