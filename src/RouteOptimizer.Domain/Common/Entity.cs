namespace RouteOptimizer.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    private List<IDomainEvent> _domainEvents = [];
    public TId Id { get; protected set; }
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected Entity(TId id)
    {
        Id = id;
    }
    
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
}