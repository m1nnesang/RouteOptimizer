namespace RouteOptimizer.Dispatcher.Wpf.Models;

public enum HandoverType
{
    TransferAll,
    Distribute,
    ReturnToPool
}

public record CreateRouteRequest(IReadOnlyList<Guid> OrderIds);

public record OptimizeRouteRequest(DateOnly RouteDate);

public record AssignRouteRequest(Guid ShiftId);

public record InsertUrgentOrderRequest(Guid OrderId);

public record ShiftStopsAssignment(Guid ShiftId, IReadOnlyList<Guid> StopIds);

public record HandoverRouteRequest(
    HandoverType Type,
    Guid? TargetShiftId,
    IReadOnlyList<ShiftStopsAssignment>? Assignments);

public record CreateVehicleRequest(
    Guid WarehouseId,
    string Type,
    decimal MaxWeightKg,
    decimal MaxVolumeM3,
    string LicenseCategory,
    IReadOnlyList<string> AllowedCargoTypes);

public record UpdateVehicleRequest(
    string Type,
    decimal MaxWeightKg,
    decimal MaxVolumeM3,
    string LicenseCategory,
    IReadOnlyList<string> AllowedCargoTypes);

public record CreateWarehouseRequest(
    string Name,
    string City,
    string Street,
    string PostalCode,
    string Country,
    double Latitude,
    double Longitude);

public record UpdateWarehouseRequest(
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country);
