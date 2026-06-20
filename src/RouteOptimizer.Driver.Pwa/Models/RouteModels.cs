namespace RouteOptimizer.Driver.Pwa.Models;

public sealed record RouteListItem(
    Guid Id,
    Guid WarehouseId,
    string Status,
    int StopsCount,
    Guid? AssignedShiftId,
    DateOnly? Date,
    string? DriverName,
    string WarehouseName);

public sealed record RouteDetail(
    Guid Id,
    Guid WarehouseId,
    Guid? AssignedShiftId,
    string Status,
    IReadOnlyList<RouteStop> Stops);

public sealed record RouteStop(
    Guid Id,
    int Sequence,
    string City,
    string Street,
    double Latitude,
    double Longitude,
    string Status,
    IReadOnlyList<Guid> OrderIds,
    IReadOnlyList<StopOrder> Orders);

public sealed record StopOrder(
    Guid OrderId,
    string Recipient,
    string? Apartment,
    string Type,
    string Phone);
