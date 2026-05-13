using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities;

public class Vehicle : AggregateRoot<Guid>
{
    public Guid WarehouseId { get; }
    public string Type { get; }
    
    Weight MaxWeightKg { get; }
    Volume MaxVolumeM3 { get; }
    LicenseCategory LicenseCategory { get; }
    
    private IReadOnlyList<CargoType> AllowedCargoTypes { get; }
    
    private Vehicle(Guid id, Guid warehouseId, string type, Weight maxWeightKg, Volume maxVolumeM3, LicenseCategory licenseCategory, IReadOnlyList<CargoType> allowedCargoTypes) : base(id)
        => (WarehouseId, Type, MaxWeightKg, MaxVolumeM3, LicenseCategory, AllowedCargoTypes) = (warehouseId, type, maxWeightKg, maxVolumeM3, licenseCategory, allowedCargoTypes);

    public static Result<Vehicle> Create(Guid warehouseId, string type, Weight maxWeightKg, Volume maxVolumeM3,
        LicenseCategory licenseCategory, IReadOnlyList<CargoType> allowedCargoTypes)
    {
        if (string.IsNullOrWhiteSpace(type) || warehouseId == Guid.Empty || allowedCargoTypes.Count == 0)
            return Result<Vehicle>.Failure("All parameters need to be filled");

        return Result<Vehicle>.Success(new Vehicle(Guid.NewGuid(), warehouseId, type, maxWeightKg, maxVolumeM3,
            licenseCategory, allowedCargoTypes));
    }
}