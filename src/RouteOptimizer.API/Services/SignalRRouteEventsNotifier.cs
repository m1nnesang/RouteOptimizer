using Microsoft.AspNetCore.SignalR;
using RouteOptimizer.API.Hubs;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.API.Services;

public class SignalRRouteEventsNotifier : IRouteEventsNotifier
{
    public const string RouteStartedEvent = "RouteStarted";
    public const string StopCompletedEvent = "StopCompleted";
    public const string StopFailedEvent = "StopFailed";
    public const string StopSkippedEvent = "StopSkipped";
    public const string RouteChangedEvent = "RouteChanged";
    public const string DriverLocationEvent = "DriverLocation";

    private readonly IHubContext<RouteHub> _hubContext;

    public SignalRRouteEventsNotifier(IHubContext<RouteHub> hubContext) => _hubContext = hubContext;

    public Task RouteStartedAsync(Guid warehouseId, Guid routeId, CancellationToken ct) =>
        _hubContext.Clients.Group(RouteHub.WarehouseGroup(warehouseId))
            .SendAsync(RouteStartedEvent, new { routeId }, ct);

    public Task StopCompletedAsync(Guid warehouseId, Guid routeId, Guid stopId, Guid? nextStopId, CancellationToken ct) =>
        _hubContext.Clients.Group(RouteHub.WarehouseGroup(warehouseId))
            .SendAsync(StopCompletedEvent, new { routeId, stopId, nextStopId }, ct);

    public Task StopFailedAsync(Guid warehouseId, Guid routeId, Guid stopId, Guid? nextStopId, CancellationToken ct) =>
        _hubContext.Clients.Group(RouteHub.WarehouseGroup(warehouseId))
            .SendAsync(StopFailedEvent, new { routeId, stopId, nextStopId }, ct);

    public Task StopSkippedAsync(Guid warehouseId, Guid routeId, Guid stopId, Guid? nextStopId, CancellationToken ct) =>
        _hubContext.Clients.Group(RouteHub.WarehouseGroup(warehouseId))
            .SendAsync(StopSkippedEvent, new { routeId, stopId, nextStopId }, ct);

    public Task RouteChangedAsync(Guid warehouseId, Guid routeId, CancellationToken ct) =>
        _hubContext.Clients
            .Groups(RouteHub.WarehouseGroup(warehouseId), RouteHub.RouteGroup(routeId))
            .SendAsync(RouteChangedEvent, new { routeId }, ct);

    public Task DriverLocationAsync(Guid warehouseId, Guid routeId, Guid driverId,
        double latitude, double longitude, DateTimeOffset timestamp, CancellationToken ct) =>
        _hubContext.Clients.Group(RouteHub.WarehouseGroup(warehouseId))
            .SendAsync(DriverLocationEvent, new { routeId, driverId, latitude, longitude, timestamp }, ct);
}
