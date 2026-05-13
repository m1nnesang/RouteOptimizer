using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Stop;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities.Route;

public class Stop : Entity<Guid>
{
    public Guid RouteId { get; }
    public Address Address { get; }
    public GeoCoordinate Location { get; }
    public DeliveryWindow? DeliveryWindow { get; }
    
    public StopStatus Status  {get; private set; } = StopStatus.Pending;
    public int Sequence { get; }
    public IReadOnlyList<Guid> Orders { get; }
    
    private Stop(Guid id, Guid routeId, Address address, GeoCoordinate location, DeliveryWindow? deliveryWindow, int sequence, IReadOnlyList<Guid> orders) : base(id)
        => (RouteId, Address, Location, DeliveryWindow, Sequence, Orders) = (routeId, address, location, deliveryWindow, sequence, orders);
    
    public static Result<Stop> Create(Guid routeId, Address address, GeoCoordinate location,
        DeliveryWindow? deliveryWindow, int sequenceNumber, IReadOnlyList<Guid> orders)
    {
        if (routeId == Guid.Empty)
            return Result<Stop>.Failure("Stop need a route");

        if (sequenceNumber < 0)
            return Result<Stop>.Failure("Sequence cannot be below 0");
        
        return Result<Stop>.Success(new Stop(Guid.NewGuid(), routeId, address, location, deliveryWindow, sequenceNumber, orders));
    }

    private void EnsureInProgress()
    {
        if (Status != StopStatus.InProgress)
            throw new InvalidOperationException("Stop is not in progress");   
    }

    public void Start()
    {
        if (Status != StopStatus.Pending)
            throw new InvalidOperationException("Stop is already started");
        
        Status = StopStatus.InProgress;
    }

    public void Complete()
    {
        EnsureInProgress();
        
        Status = StopStatus.Completed;
        AddDomainEvent(new StopCompleted(Id, RouteId , true));
    }
    
    public void PartiallyComplete()
    {
        EnsureInProgress();
        
        Status = StopStatus.PartiallyCompleted;
        AddDomainEvent(new StopCompleted(Id, RouteId , false));
    }
    
    public void Skip()
    {
        if (Status == StopStatus.Skipped)
            throw new InvalidOperationException("Stop is already skipped");
        
        if (Status != StopStatus.InProgress && Status != StopStatus.Pending)
            throw new InvalidOperationException("Cannot skip a completed stop");
        
        Status = StopStatus.Skipped;
        AddDomainEvent(new StopSkipped(Id , RouteId));
    }

    public void Resume()
    {
        if (Status != StopStatus.Skipped)
            throw new InvalidOperationException("Stop is not skipped");
        
        Status = StopStatus.InProgress;  
    }

    public void Failed()
    {
        EnsureInProgress();

        Status = StopStatus.Failed;
        AddDomainEvent(new StopFailed(Id, RouteId));
    }
    
}