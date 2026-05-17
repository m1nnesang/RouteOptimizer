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
    
    private BusinessOrder(Guid id, Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, DeliveryWindow deliveryWindow ,PhoneNumber number ,CargoType cargoType, string? notes,
        string companyName, string contactPerson) 
        : base(id, warehouseId, address, location, weight, volume, deliveryWindow ,number , cargoType, notes) => 
        (CompanyName, ContactPerson, RequiresSignature) = (companyName, contactPerson, true);

    public static Result<BusinessOrder> Create(
        Guid warehouseId, Address address, GeoCoordinate location,
        Weight weight, Volume volume, DeliveryWindow deliveryWindow ,PhoneNumber number ,CargoType cargoType, string? notes,
        string companyName, string contactPerson)
    {
        if (string.IsNullOrWhiteSpace(companyName)|| string.IsNullOrWhiteSpace(contactPerson))
            return Result<BusinessOrder>.Failure("Company name and contact person are required");

        var order = new BusinessOrder(Guid.NewGuid(), warehouseId, address, location, weight, volume, deliveryWindow , number, cargoType,
            notes, companyName, contactPerson);

        order.AddDomainEvent(new OrderCreated(order.Id, order.WarehouseId));
        
        return Result<BusinessOrder>.Success(order);
    }
}