using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Vehicles.GetVehicles;

public record VehicleListItemDto(
    Guid Id,
    Guid WarehouseId,
    string Type,
    decimal MaxWeightKg,
    decimal MaxVolumeM3,
    LicenseCategory LicenseCategory,
    IReadOnlyList<CargoType> AllowedCargoTypes);
