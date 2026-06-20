using MediatR;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Events.Route;

namespace RouteOptimizer.Application.Events;

public abstract class RouteChangedNotifierBase
{
    private readonly IRouteRepository _routeRepository;
    private readonly IRouteEventsNotifier _notifier;
    private readonly ILogger _logger;

    protected RouteChangedNotifierBase(
        IRouteRepository routeRepository, IRouteEventsNotifier notifier, ILogger logger)
    {
        _routeRepository = routeRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected async Task BroadcastRouteChangedAsync(Guid routeId, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(routeId, ct);
        if (route is null) return;

        try
        {
            await _notifier.RouteChangedAsync(route.WarehouseId, route.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for RouteChanged, route {RouteId}", route.Id);
        }
    }
}

public sealed class UrgentOrderInsertedNotificationHandler
    : RouteChangedNotifierBase, INotificationHandler<UrgentOrderInserted>
{
    public UrgentOrderInsertedNotificationHandler(
        IRouteRepository routeRepository, IRouteEventsNotifier notifier,
        ILogger<UrgentOrderInsertedNotificationHandler> logger)
        : base(routeRepository, notifier, logger) { }

    public Task Handle(UrgentOrderInserted notification, CancellationToken ct) =>
        BroadcastRouteChangedAsync(notification.RouteId, ct);
}

public sealed class RouteInterruptedNotificationHandler
    : RouteChangedNotifierBase, INotificationHandler<RouteInterrupted>
{
    public RouteInterruptedNotificationHandler(
        IRouteRepository routeRepository, IRouteEventsNotifier notifier,
        ILogger<RouteInterruptedNotificationHandler> logger)
        : base(routeRepository, notifier, logger) { }

    public Task Handle(RouteInterrupted notification, CancellationToken ct) =>
        BroadcastRouteChangedAsync(notification.RouteId, ct);
}

public sealed class RouteAssignedNotificationHandler
    : RouteChangedNotifierBase, INotificationHandler<RouteAssignedToDriver>
{
    public RouteAssignedNotificationHandler(
        IRouteRepository routeRepository, IRouteEventsNotifier notifier,
        ILogger<RouteAssignedNotificationHandler> logger)
        : base(routeRepository, notifier, logger) { }

    public Task Handle(RouteAssignedToDriver notification, CancellationToken ct) =>
        BroadcastRouteChangedAsync(notification.RouteId, ct);
}
