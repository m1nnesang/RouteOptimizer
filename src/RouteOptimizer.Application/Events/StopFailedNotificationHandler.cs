using MediatR;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Events.Stop;

namespace RouteOptimizer.Application.Events;

public class StopFailedNotificationHandler : INotificationHandler<StopFailed>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRouteEventsNotifier _notifier;
    private readonly ILogger<StopFailedNotificationHandler> _logger;

    public StopFailedNotificationHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IRouteEventsNotifier notifier,
        ILogger<StopFailedNotificationHandler> logger)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(StopFailed notification, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(notification.RouteId, ct);
        if (route is null) return;

        var failedStop = route.Stops.FirstOrDefault(s => s.Id == notification.StopId);
        if (failedStop is not null)
        {
            var failedOrders = await _orderRepository.GetByIdsAsync(failedStop.Orders, ct);
            foreach (var order in failedOrders)
            {
                try { order.MarkAsFailed(); }
                catch (InvalidOperationException) { }
            }
        }

        var nextStop = route.AdvanceToNextStop();
        if (nextStop is not null)
        {
            var nextOrders = await _orderRepository.GetByIdsAsync(nextStop.Orders, ct);
            foreach (var order in nextOrders)
            {
                try { order.MarkAsInTransit(); }
                catch (InvalidOperationException) { }
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            await _notifier.StopFailedAsync(route.WarehouseId, route.Id, notification.StopId, nextStop?.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for StopFailed, stop {StopId}", notification.StopId);
        }
    }
}
