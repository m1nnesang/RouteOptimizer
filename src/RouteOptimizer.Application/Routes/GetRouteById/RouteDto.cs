using RouteOptimizer.Application.Routes.Stops;

namespace RouteOptimizer.Application.Routes.GetRouteById;

public record RouteDto(
    Guid Id,
    Guid WarehouseId,
    Guid? AssignedShiftId,
    string Status,
    IReadOnlyList<StopDto> Stops,
    IReadOnlyList<RoutePoint> Geometry);

public sealed record RoutePoint(double Latitude, double Longitude);
