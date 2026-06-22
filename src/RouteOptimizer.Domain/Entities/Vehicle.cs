using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities;

public class Vehicle : AggregateRoot<Guid>
{
    private Vehicle() : base(default)
    {
        Type = null!;
        MaxWeightKg = null!;
        MaxVolumeM3 = null!;
        AllowedCargoTypes = [];
    }

    private Vehicle(Guid id, Guid warehouseId, string type, Weight maxWeightKg, Volume maxVolumeM3,
        LicenseCategory licenseCategory, IReadOnlyList<CargoType> allowedCargoTypes) : base(id)
    {
        (WarehouseId, Type, MaxWeightKg, MaxVolumeM3, LicenseCategory, AllowedCargoTypes) = (warehouseId, type,
            maxWeightKg, maxVolumeM3, licenseCategory, allowedCargoTypes);
    }

    public Guid WarehouseId { get; }
    public string Type { get; private set; }

    public Weight MaxWeightKg { get; private set; }
    public Volume MaxVolumeM3 { get; private set; }
    public LicenseCategory LicenseCategory { get; private set; }

    public IReadOnlyList<CargoType> AllowedCargoTypes { get; private set; }

    public static Result<Vehicle> Create(Guid warehouseId, string type, Weight maxWeightKg, Volume maxVolumeM3,
        LicenseCategory licenseCategory, IReadOnlyList<CargoType> allowedCargoTypes)
    {
        if (string.IsNullOrWhiteSpace(type) || warehouseId == Guid.Empty || allowedCargoTypes.Count == 0)
            return Result<Vehicle>.Failure("All parameters need to be filled");

        return Result<Vehicle>.Success(new Vehicle(Guid.NewGuid(), warehouseId, type, maxWeightKg, maxVolumeM3,
            licenseCategory, allowedCargoTypes));
    }

    public Result Update(string type, Weight maxWeightKg, Volume maxVolumeM3,
        LicenseCategory licenseCategory, IReadOnlyList<CargoType> allowedCargoTypes)
    {
        if (string.IsNullOrWhiteSpace(type) || allowedCargoTypes.Count == 0)
            return Result.Failure("All parameters need to be filled");

        Type = type;
        MaxWeightKg = maxWeightKg;
        MaxVolumeM3 = maxVolumeM3;
        LicenseCategory = licenseCategory;
        AllowedCargoTypes = allowedCargoTypes;

        return Result.Success();
    }
}
