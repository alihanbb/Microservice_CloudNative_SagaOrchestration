using MediatR;

namespace OrderServices.Domain.SeedWork;

public abstract class OrderEntity
{
    private int? _requestedHashCode;
    private int _id;
    
    public virtual int Id
    {
        get => _id;
        protected set => _id = value;
    }

    private List<INotification>? _domainEvents;
    public IReadOnlyCollection<INotification>? DomainEvents => _domainEvents?.AsReadOnly();

    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents ??= new List<INotification>();
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem)
    {
        _domainEvents?.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public bool IsTransient() => Id == default;

    public override bool Equals(object? obj)
    {
        if (obj is not OrderEntity entity)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetType() != obj.GetType())
            return false;

        if (entity.IsTransient() || IsTransient())
            return false;

        return entity.Id == Id;
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            _requestedHashCode ??= Id.GetHashCode() ^ 31;
            return _requestedHashCode.Value;
        }
        
        return base.GetHashCode();
    }

    public static bool operator ==(OrderEntity? left, OrderEntity? right)
    {
        if (left is null)
            return right is null;
        
        return left.Equals(right);
    }

    public static bool operator !=(OrderEntity? left, OrderEntity? right)
    {
        return !(left == right);
    }
}
