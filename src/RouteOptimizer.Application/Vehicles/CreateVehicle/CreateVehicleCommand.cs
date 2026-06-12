using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Vehicles.CreateVehicle;

public record CreateVehicleCommand(
    Guid WarehouseId,
    string Type,
    decimal MaxWeightKg,
    decimal MaxVolumeM3,
    LicenseCategory LicenseCategory,
    IReadOnlyList<CargoType> AllowedCargoTypes) : ICommand<Result<Guid>>;
