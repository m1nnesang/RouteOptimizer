namespace RouteOptimizer.Application.Abstractions;

public interface IRouteEventsNotifier
{
    Task RouteStartedAsync(Guid warehouseId, Guid routeId, CancellationToken ct);
    Task StopCompletedAsync(Guid warehouseId, Guid routeId, Guid stopId, Guid? nextStopId, CancellationToken ct);
    Task StopFailedAsync(Guid warehouseId, Guid routeId, Guid stopId, Guid? nextStopId, CancellationToken ct);
    Task StopSkippedAsync(Guid warehouseId, Guid routeId, Guid stopId, Guid? nextStopId, CancellationToken ct);
    Task RouteChangedAsync(Guid warehouseId, Guid routeId, CancellationToken ct);
    Task RouteAssignedToDriverAsync(Guid driverId, Guid routeId, CancellationToken ct);
    Task DriverLocationAsync(Guid warehouseId, Guid routeId, Guid driverId, double latitude, double longitude, DateTimeOffset timestamp, CancellationToken ct);
}
