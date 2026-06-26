using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Routes.Orders.DeliverOrder;

public class DeliverOrderHandler : IRequestHandler<DeliverOrderCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;
    private readonly ICurrentUser _currentUser;

    public DeliverOrderHandler(IRouteRepository routeRepository, IOrderRepository orderRepository, IDriverShiftRepository shiftRepository, IDeliveryAttemptRepository deliveryAttemptRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _shiftRepository = shiftRepository;
        _deliveryAttemptRepository = deliveryAttemptRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeliverOrderCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (!await DriverRouteAccess.IsAssignedToDriverAsync(_shiftRepository, route, _currentUser.UserId, ct))
            throw new NotFoundException("Route not found");

        if (route.Status != RouteStatus.InProgress)
            return Result.Failure("Route is done");

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId);

        if (stop is null)
            throw new NotFoundException("Stop is not found");

        if (stop.Status != StopStatus.InProgress)
            return Result.Failure("Stop is not in progress");

        if (!stop.Orders.Contains(request.OrderId))
            throw new NotFoundException("Order is not in this stop");

        var stopOrders = await _orderRepository.GetByIdsAsync(stop.Orders, ct);
        var order = stopOrders.FirstOrDefault(o => o.Id == request.OrderId);

        if (order is null)
            throw new NotFoundException("Order is not found");

        try
        {
            order.MarkAsDelivered();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        if (!string.IsNullOrWhiteSpace(request.PhotoKey))
        {
            var location = GeoCoordinate.Create(request.Latitude ?? 0, request.Longitude ?? 0);

            if (location.IsSuccess)
            {
                var attempt = DeliveryAttempt.Create(order.Id, _currentUser.UserId, route.Id, location.Value!,
                    DeliveryOutcome.Delivered, null, null, request.PhotoKey);

                if (attempt.IsSuccess)
                    await _deliveryAttemptRepository.AddAsync(attempt.Value!, ct);
            }
        }

        StopResolution.ResolveIfComplete(stop, stopOrders);

        return Result.Success();
    }
}
