using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Order;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities.Orders;

public class BusinessOrder : Order
{
    public string CompanyName { get; }
    public string? ContactPerson { get; }
    public bool RequiresSignature { get; }
    public DeliveryWindow TimeWindow { get; }
    
    private BusinessOrder(Guid id, Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, PhoneNumber number ,CargoType cargoType, string? notes,
        string companyName, string contactPerson, DeliveryWindow timeWindow) 
        : base(id, warehouseId, address, location, weight, volume, number , cargoType, notes) => 
        (CompanyName, ContactPerson, TimeWindow, RequiresSignature) = (companyName, contactPerson, timeWindow , true);

    public static Result<BusinessOrder> Create(
        Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, PhoneNumber number ,CargoType cargoType, string? notes,
        string companyName, string contactPerson, DeliveryWindow timeWindow)
    {
        if (string.IsNullOrWhiteSpace(companyName)|| string.IsNullOrWhiteSpace(contactPerson))
            return Result<BusinessOrder>.Failure("Company name and contact person are required");

        var order = new BusinessOrder(Guid.NewGuid(), warehouseId, address, location, weight, volume, number, cargoType,
            notes, companyName, contactPerson, timeWindow);

        order.AddDomainEvent(new OrderCreated(order.Id, order.WarehouseId));
        
        return Result<BusinessOrder>.Success(order);
    }
}