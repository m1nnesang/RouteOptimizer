using MediatR;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Events.Stop;

namespace RouteOptimizer.Application.Events;

public class StopSkippedNotificationHandler : INotificationHandler<StopSkipped>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRouteEventsNotifier _notifier;
    private readonly ILogger<StopSkippedNotificationHandler> _logger;

    public StopSkippedNotificationHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IRouteEventsNotifier notifier,
        ILogger<StopSkippedNotificationHandler> logger)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(StopSkipped notification, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(notification.RouteId, ct);
        if (route is null) return;

        var nextStop = route.AdvanceToNextStop();

        if (nextStop is not null)
        {
            var nextOrders = await _orderRepository.GetByIdsAsync(nextStop.Orders, ct);
            foreach (var order in nextOrders)
            {
                try { order.MarkAsInTransit(); }
                catch (InvalidOperationException) { }
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        try
        {
            await _notifier.StopSkippedAsync(route.WarehouseId, route.Id, notification.StopId, nextStop?.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for StopSkipped, stop {StopId}", notification.StopId);
        }
    }
}
