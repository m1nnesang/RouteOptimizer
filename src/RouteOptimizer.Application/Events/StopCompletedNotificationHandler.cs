using MediatR;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Events.Stop;

namespace RouteOptimizer.Application.Events;

public class StopCompletedNotificationHandler : INotificationHandler<StopCompleted>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRouteEventsNotifier _notifier;
    private readonly ILogger<StopCompletedNotificationHandler> _logger;

    public StopCompletedNotificationHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IRouteEventsNotifier notifier,
        ILogger<StopCompletedNotificationHandler> logger)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(StopCompleted notification, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(notification.RouteId, ct);
        if (route is null) return;

        if (!notification.IsPartial)
        {
            var completedStop = route.Stops.FirstOrDefault(s => s.Id == notification.StopId);
            if (completedStop is not null)
            {
                var completedOrders = await _orderRepository.GetByIdsAsync(completedStop.Orders, ct);
                foreach (var order in completedOrders)
                {
                    try { order.MarkAsDelivered(); }
                    catch (InvalidOperationException) { }
                }
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
            await _notifier.StopCompletedAsync(route.WarehouseId, route.Id, notification.StopId, nextStop?.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for StopCompleted, stop {StopId}", notification.StopId);
        }
    }
}
