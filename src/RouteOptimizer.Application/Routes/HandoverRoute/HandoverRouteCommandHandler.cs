using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.HandoverRoute;

public class HandoverRouteCommandHandler : IRequestHandler<HandoverRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IEnumerable<IRouteOptimizer> _optimizers;
    private readonly IDistanceMatrixProvider _distanceMatrixProvider;
    private readonly IUnitOfWork _unitOfWork;

    public HandoverRouteCommandHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IWarehouseRepository warehouseRepository,
        IDriverShiftRepository shiftRepository,
        IEnumerable<IRouteOptimizer> optimizers,
        IDistanceMatrixProvider distanceMatrixProvider,
        IUnitOfWork unitOfWork)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _warehouseRepository = warehouseRepository;
        _shiftRepository = shiftRepository;
        _optimizers = optimizers;
        _distanceMatrixProvider = distanceMatrixProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(HandoverRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);
        if (route is null) return Result.Failure("Route not found");

        if (route.Status is not (RouteStatus.InProgress or RouteStatus.Assigned or RouteStatus.Interrupted))
            return Result.Failure("Route must be in progress, assigned or interrupted to be handed over");

        var warehouse = await _warehouseRepository.GetByIdAsync(route.WarehouseId, ct);
        if (warehouse is null) return Result.Failure("Warehouse not found");

        if (route.Status != RouteStatus.Interrupted)
            route.Interrupt();

        var remainingStops = route.RemainingStops;

        return request.Type switch
        {
            HandoverType.TransferAll  => await HandleTransferAllAsync(route, remainingStops, warehouse, request.TargetShiftId!.Value, ct),
            HandoverType.Distribute   => await HandleDistributeAsync(route, remainingStops, warehouse, request.Assignments!, ct),
            HandoverType.ReturnToPool => await HandleReturnToPoolAsync(remainingStops, ct),
            _                         => Result.Failure("Unknown handover type")
        };
    }

    private async Task<Result> HandleTransferAllAsync(
        Route route,
        IReadOnlyList<Stop> remainingStops,
        Warehouse warehouse,
        Guid targetShiftId,
        CancellationToken ct)
    {
        var shift = await _shiftRepository.GetByIdAsync(targetShiftId, ct);
        if (shift is null) return Result.Failure("Target shift not found");
        if (shift.WarehouseId != route.WarehouseId)
            return Result.Failure("Target shift belongs to a different warehouse");

        var createResult = await CreateOptimizedRouteAsync(route.WarehouseId, remainingStops, warehouse, ct);
        if (createResult.IsFailure) return Result.Failure(createResult.Error!);

        var newRoute = createResult.Value!;

        var orderIds = remainingStops.SelectMany(s => s.Orders).Distinct();
        var orders = await _orderRepository.GetByIdsAsync(orderIds, ct);
        foreach (var order in orders)
            order.Reassign(newRoute.Id);

        newRoute.AssignShift(targetShiftId);
        await _routeRepository.AddAsync(newRoute, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task<Result> HandleDistributeAsync(
        Route route,
        IReadOnlyList<Stop> remainingStops,
        Warehouse warehouse,
        IReadOnlyList<ShiftStopsAssignment> assignments,
        CancellationToken ct)
    {
        var assignedStopIds = assignments.SelectMany(a => a.StopIds).ToHashSet();
        var remainingStopIds = remainingStops.Select(s => s.Id).ToHashSet();

        if (!assignedStopIds.IsSubsetOf(remainingStopIds))
            return Result.Failure("Some stop IDs do not belong to this route's remaining stops");

        var allOrderIds = remainingStops.SelectMany(s => s.Orders).Distinct();
        var allOrders = (await _orderRepository.GetByIdsAsync(allOrderIds, ct)).ToDictionary(o => o.Id);

        var newRoutes = new List<(Route NewRoute, Guid ShiftId, IReadOnlyList<Guid> StopIds)>();

        foreach (var assignment in assignments)
        {
            var shift = await _shiftRepository.GetByIdAsync(assignment.ShiftId, ct);
            if (shift is null) return Result.Failure($"Shift {assignment.ShiftId} not found");
            if (shift.WarehouseId != route.WarehouseId)
                return Result.Failure($"Shift {assignment.ShiftId} belongs to a different warehouse");

            var stopsForShift = remainingStops.Where(s => assignment.StopIds.Contains(s.Id)).ToList();
            var createResult = await CreateOptimizedRouteAsync(route.WarehouseId, stopsForShift, warehouse, ct);
            if (createResult.IsFailure) return Result.Failure(createResult.Error!);

            newRoutes.Add((createResult.Value!, assignment.ShiftId, assignment.StopIds));
        }

        foreach (var (newRoute, shiftId, stopIds) in newRoutes)
        {
            var routeOrderIds = remainingStops
                .Where(s => stopIds.Contains(s.Id))
                .SelectMany(s => s.Orders);

            foreach (var orderId in routeOrderIds)
                if (allOrders.TryGetValue(orderId, out var order))
                    order.Reassign(newRoute.Id);

            newRoute.AssignShift(shiftId);
            await _routeRepository.AddAsync(newRoute, ct);
        }

        var unassignedStops = remainingStops.Where(s => !assignedStopIds.Contains(s.Id));
        foreach (var stop in unassignedStops)
            foreach (var orderId in stop.Orders)
                if (allOrders.TryGetValue(orderId, out var order))
                    order.ReturnToPool();

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result> HandleReturnToPoolAsync(IReadOnlyList<Stop> remainingStops, CancellationToken ct)
    {
        var orderIds = remainingStops.SelectMany(s => s.Orders).Distinct();
        var orders = await _orderRepository.GetByIdsAsync(orderIds, ct);
        foreach (var order in orders)
            order.ReturnToPool();

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<Route>> CreateOptimizedRouteAsync(
        Guid warehouseId,
        IReadOnlyList<Stop> sourceStops,
        Warehouse warehouse,
        CancellationToken ct)
    {
        if (!_optimizers.Any()) return Result<Route>.Failure("No optimizers configured");

        var routeResult = Route.Create(warehouseId);
        if (routeResult.IsFailure) return routeResult;

        var newRoute = routeResult.Value!;

        for (var i = 0; i < sourceStops.Count; i++)
        {
            var src = sourceStops[i];
            var stopResult = Stop.Create(newRoute.Id, src.Address, src.Location, src.DeliveryWindow, i, src.Orders);
            if (stopResult.IsFailure) return Result<Route>.Failure(stopResult.Error!);
            newRoute.AddStop(stopResult.Value!);
        }

        var warehouseCoords = (warehouse.Location.Latitude, warehouse.Location.Longitude);
        var stopsCoords = newRoute.Stops.Select(s => (s.Location.Latitude, s.Location.Longitude)).ToList();
        var matrix = await _distanceMatrixProvider.GetMatrixAsync(warehouseCoords, stopsCoords, ct);

        var input = new RouteOptimizerInput
        {
            Warehouse = warehouseCoords,
            Stops = newRoute.Stops.Select(s => new StopInput(
                s.Id,
                s.Location.Latitude,
                s.Location.Longitude,
                s.DeliveryWindow is { Start: not null, End: not null } dw
                    ? new TimeWindow(dw.Start.Value, dw.End.Value) : null)).ToList(),
            Matrix = matrix,
            RouteDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var results = await Task.WhenAll(_optimizers.Select(o => o.OptimizeAsync(input, ct)));
        var best = results.MinBy(r => r.TotalDurationSeconds)!;

        newRoute.ApplyOptimizedOrders(best.OrderedStopIds);
        newRoute.Optimize();

        return Result<Route>.Success(newRoute);
    }
}
