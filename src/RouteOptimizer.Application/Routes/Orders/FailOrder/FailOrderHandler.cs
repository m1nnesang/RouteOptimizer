using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Routes.Orders.FailOrder;

public class FailOrderHandler : IRequestHandler<FailOrderCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDriverShiftRepository _driverShiftRepository;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;

    public FailOrderHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IDriverShiftRepository driverShiftRepository,
        IDeliveryAttemptRepository deliveryAttemptRepository)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _driverShiftRepository = driverShiftRepository;
        _deliveryAttemptRepository = deliveryAttemptRepository;
    }

    public async Task<Result> Handle(FailOrderCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
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

        var shift = await _driverShiftRepository.GetByIdAsync(route.AssignedShiftId!.Value, ct);

        if (shift is null)
            throw new NotFoundException("Shift not found");

        if (shift.DriverId != request.DriverId)
            return Result.Failure("Access denied");

        var coordResult = GeoCoordinate.Create(request.Latitude, request.Longitude);

        if (coordResult.IsFailure)
            return Result.Failure(coordResult.Error!);

        var stopOrders = await _orderRepository.GetByIdsAsync(stop.Orders, ct);
        var order = stopOrders.FirstOrDefault(o => o.Id == request.OrderId);

        if (order is null)
            throw new NotFoundException("Order is not found");

        var attempt = DeliveryAttempt.Create(order.Id, shift.DriverId, route.Id, coordResult.Value!,
            DeliveryOutcome.Failed, request.FailureReason, request.Notes, request.PhotoKey);

        if (attempt.IsFailure)
            return Result.Failure(attempt.Error!);

        await _deliveryAttemptRepository.AddAsync(attempt.Value!, ct);

        try
        {
            order.MarkAsFailed();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        StopResolution.ResolveIfComplete(stop, stopOrders);

        return Result.Success();
    }
}
