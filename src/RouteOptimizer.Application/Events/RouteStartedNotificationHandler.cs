using MediatR;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Events.Route;

namespace RouteOptimizer.Application.Events;

public class RouteStartedNotificationHandler : INotificationHandler<RouteStarted>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRouteEventsNotifier _notifier;
    private readonly ILogger<RouteStartedNotificationHandler> _logger;

    public RouteStartedNotificationHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IRouteEventsNotifier notifier,
        ILogger<RouteStartedNotificationHandler> logger)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(RouteStarted notification, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(notification.RouteId, ct);
        if (route is null) return;

        var firstStop = route.StartFirstPendingStop();

        if (firstStop is not null)
        {
            var orders = await _orderRepository.GetByIdsAsync(firstStop.Orders, ct);
            foreach (var order in orders)
            {
                try { order.MarkAsInTransit(); }
                catch (InvalidOperationException) { }
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        try
        {
            await _notifier.RouteStartedAsync(route.WarehouseId, route.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for RouteStarted, route {RouteId}", route.Id);
        }
    }
}
