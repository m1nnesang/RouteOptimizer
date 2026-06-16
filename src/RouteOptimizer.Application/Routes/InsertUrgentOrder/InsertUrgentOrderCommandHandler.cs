using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.InsertUrgentOrder;

public class InsertUrgentOrderCommandHandler : IRequestHandler<InsertUrgentOrderCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IEnumerable<IRouteOptimizer> _optimizers;
    private readonly IDistanceMatrixProvider _distanceMatrixProvider;
    private readonly ICurrentUser _currentUser;

    public InsertUrgentOrderCommandHandler(
        IRouteRepository routeRepository,
        IOrderRepository orderRepository,
        IWarehouseRepository warehouseRepository,
        IEnumerable<IRouteOptimizer> optimizers,
        IDistanceMatrixProvider distanceMatrixProvider,
        ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _warehouseRepository = warehouseRepository;
        _optimizers = optimizers;
        _distanceMatrixProvider = distanceMatrixProvider;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(InsertUrgentOrderCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);
        if (route is null) return Result.Failure("Route not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            return Result.Failure("Route not found");

        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result.Failure("Order not found");

        var insertResult = route.InsertUrgentOrder(order);
        if (insertResult.IsFailure) return insertResult;

        var newStop = route.Stops.Single(s => s.Orders.Contains(order.Id));
        await _routeRepository.AddStopAsync(newStop, ct);

        var warehouse = await _warehouseRepository.GetByIdAsync(route.WarehouseId, ct);
        if (warehouse is null) return Result.Failure("Warehouse not found");

        var remainingStops = route.Stops
            .Where(s => s.Status != StopStatus.Completed && s.Status != StopStatus.PartiallyCompleted)
            .ToList();

        var warehouseCoords = (warehouse.Location.Latitude, warehouse.Location.Longitude);
        var stopsCoords = remainingStops.Select(s => (s.Location.Latitude, s.Location.Longitude)).ToList();
        var matrix = await _distanceMatrixProvider.GetMatrixAsync(warehouseCoords, stopsCoords, ct);

        var input = new RouteOptimizerInput
        {
            Warehouse = warehouseCoords,
            Stops = remainingStops.Select(s => new StopInput(s.Id, s.Location.Latitude, s.Location.Longitude,
                s.DeliveryWindow is { Start: not null, End: not null } dw
                    ? new TimeWindow(dw.Start.Value, dw.End.Value) : null)).ToList(),
            Matrix = matrix,
            RouteDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        if (!_optimizers.Any()) return Result.Failure("No optimizers configured");

        var results = await Task.WhenAll(_optimizers.Select(o => o.OptimizeAsync(input, ct)));
        var best = results.MinBy(r => r.TotalDurationSeconds)!;

        var reorderResult = route.ReorderRemainingStops(best.OrderedStopIds);
        if (reorderResult.IsFailure) return reorderResult;

        return Result.Success();
    }
}
