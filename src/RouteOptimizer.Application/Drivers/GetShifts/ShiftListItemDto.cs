namespace RouteOptimizer.Application.Drivers.GetShifts;

public record ShiftListItemDto(
    Guid Id,
    Guid DriverId,
    Guid VehicleId,
    Guid WarehouseId,
    DateOnly ShiftDate,
    DateTime? StartedAt,
    DateTime? EndedAt,
    string DriverName,
    string VehicleType,
    string WarehouseName);
