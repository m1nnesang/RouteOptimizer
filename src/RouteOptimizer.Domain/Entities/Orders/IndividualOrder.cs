using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Order;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities.Orders;

public class IndividualOrder : Order
{
    private IndividualOrder()
    {
        CustomerName = null!;
    } // EF Core

    private IndividualOrder(Guid id, Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, DeliveryWindow? deliveryWindow, PhoneNumber number, CargoType cargoType,
        string? notes,
        string customerName, bool allowLeaveAtDoor)
        : base(id, warehouseId, address, location, weight, volume, deliveryWindow, number, cargoType, notes)
    {
        (CustomerName, AllowLeaveAtDoor) = (customerName, allowLeaveAtDoor);
    }

    public string CustomerName { get; }

    public bool AllowLeaveAtDoor { get; }

    public static Result<IndividualOrder> Create(
        Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, DeliveryWindow deliveryWindow, PhoneNumber number, CargoType cargoType,
        string? notes, string customerName, bool allowLeaveAtDoor)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Result<IndividualOrder>.Failure("Customer name need to be filled");

        var order = new IndividualOrder(Guid.NewGuid(), warehouseId, address, location, weight, volume, deliveryWindow,
            number,
            cargoType, notes, customerName, allowLeaveAtDoor);

        order.AddDomainEvent(new OrderCreated(order.Id, order.WarehouseId));

        return Result<IndividualOrder>.Success(order);
    }
}