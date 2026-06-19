namespace RouteOptimizer.Dispatcher.Wpf.Models;

public enum HandoverType
{
    TransferAll,
    Distribute,
    ReturnToPool
}

public record CreateRouteRequest(IReadOnlyList<Guid> OrderIds, DateOnly Date);

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

public record CreateBusinessOrderRequest(
    Guid WarehouseId,
    string CompanyName,
    string ContactPerson,
    TimeOnly Start,
    TimeOnly End,
    string Street,
    string City,
    string Postcode,
    string Country,
    string PhoneNumber,
    decimal Weight,
    decimal Volume,
    string CargoType,
    string? Notes,
    string WindowStrictness,
    string? Apartment = null,
    double? Latitude = null,
    double? Longitude = null);

public record CreateIndividualOrderRequest(
    Guid WarehouseId,
    string Street,
    string City,
    string Postcode,
    string Country,
    string CustomerName,
    string PhoneNumber,
    decimal Weight,
    decimal Volume,
    string CargoType,
    bool AllowLeaveAtDoor,
    string? Notes,
    TimeOnly? Start,
    TimeOnly? End,
    string? WindowStrictness,
    string? Apartment = null,
    double? Latitude = null,
    double? Longitude = null);

public record CreateWarehouseRequest(
    string Name,
    string City,
    string Street,
    string PostalCode,
    string Country);

public record UpdateWarehouseRequest(
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country);
