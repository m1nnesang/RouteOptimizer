using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Order;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities.Orders;

public class IndividualOrder : Order
{
    public string CustomerName { get; }

    private bool AllowLeaveAtDoor { get; }
    
    public DeliveryWindow? TimeWindow { get; }
    
    private IndividualOrder(Guid id, Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, PhoneNumber number , CargoType cargoType, string? notes,
        string customerName, bool allowLeaveAtDoor,DeliveryWindow? timeWindow) 
        : base(id, warehouseId, address, location, weight, volume, number ,cargoType, notes) 
        => (CustomerName, AllowLeaveAtDoor, TimeWindow) = (customerName, allowLeaveAtDoor,timeWindow);

    public static Result<IndividualOrder> Create(
        Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, PhoneNumber number ,CargoType cargoType, string? notes, string customerName, bool allowLeaveAtDoor,
        DeliveryWindow? timeWindow)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Result<IndividualOrder>.Failure("Customer name need to be filled");

        var order = new IndividualOrder(Guid.NewGuid(), warehouseId, address, location, weight, volume, number,
            cargoType, notes, customerName, allowLeaveAtDoor, timeWindow);
        
        order.AddDomainEvent(new OrderCreated(order.Id, order.WarehouseId));
        
        return Result<IndividualOrder>.Success(order);
    }
}