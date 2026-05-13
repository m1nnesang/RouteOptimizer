using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Order;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities.Orders;

public abstract class Order : AggregateRoot<Guid>
{
    public Guid WarehouseId { get; }
    public Address Address { get; }
    public GeoCoordinate Location { get; }
    public Weight Weight { get; }
    public Volume Volume { get; }
    
    public PhoneNumber Number { get; }
    public CargoType CargoType { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public Guid? AssignedRouteId { get; private set; } = null;
    public string? Notes { get; }
    
    protected Order(Guid id, Guid warehouseId, Address address, GeoCoordinate location, Weight weight, Volume volume, PhoneNumber number , CargoType cargoType, string? notes) : base(id)
        => (WarehouseId, Address, Location, Weight, Volume, Number ,CargoType, Notes) = (warehouseId, address, location, weight, volume, number, cargoType, notes);

    public void AssignToRoute(Guid routeId)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be assigned to route");
    
        AssignedRouteId = routeId;
        Status = OrderStatus.AssignedToRoute;
    }
    
    public void MarkAsInTransit()
    {
        if (Status != OrderStatus.AssignedToRoute)
            throw new InvalidOperationException("Only assigned orders can be marked as in transit");
        
        Status = OrderStatus.InTransit;
    }
    
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.InTransit)
            throw new InvalidOperationException("Only in transit orders can be marked as delivered");
        
        Status = OrderStatus.Delivered;
        AddDomainEvent(new OrderDelivered(Id , AssignedRouteId ?? Guid.Empty));
    }
    
    
    public void MarkAsFailed()
    {
        if (Status != OrderStatus.InTransit)
            throw new InvalidOperationException("Only in transit orders can be marked as failed");
        
        var routeId = AssignedRouteId ?? Guid.Empty;
        
        Status = OrderStatus.Failed;
        AssignedRouteId = null;
        AddDomainEvent(new OrderFailed(Id, routeId));
    }

    public void MarkAsCancelled()
    {
        if (Status != OrderStatus.Created && Status != OrderStatus.AssignedToRoute)
            throw new InvalidOperationException("Only created orders can be cancelled");
        
        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelled(Id));
    }
    
    

}