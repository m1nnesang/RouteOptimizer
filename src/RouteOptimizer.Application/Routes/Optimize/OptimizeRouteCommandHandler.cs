using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Optimize;

public class OptimizeRouteCommandHandler : IRequestHandler<OptimizeRouteCommand, OptimizeRouteResult>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IDistanceMatrixProvider _distanceMatrixProvider;
    private readonly IEnumerable<IRouteOptimizer> _optimizers;
    private readonly ICurrentUser _currentUser;

    public OptimizeRouteCommandHandler(IRouteRepository routeRepository, IWarehouseRepository warehouseRepository, IDistanceMatrixProvider distanceMatrixProvider, IEnumerable<IRouteOptimizer> optimizers, ICurrentUser currentUser)
    {
        (_routeRepository, _warehouseRepository, _distanceMatrixProvider, _optimizers, _currentUser) = (routeRepository, warehouseRepository, distanceMatrixProvider, optimizers, currentUser);
    }

    public async Task<OptimizeRouteResult> Handle(OptimizeRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route not found");

        var warehouse = await _warehouseRepository.GetByIdAsync(route.WarehouseId, ct);

        if (warehouse is null)
            throw new NotFoundException("Warehouse not found");

        if (route.Status != RouteStatus.Draft)
            throw new InvalidOperationException("Only Draft routes can be optimized");

        var stops = route.Stops.Where(s => s.Status != StopStatus.Completed)
            .Select(s => new StopInput(s.Id, s.Location.Latitude, s.Location.Longitude,
            s.DeliveryWindow is {Start:not null, End:not null,} dw? new TimeWindow(dw.Start.Value, dw.End.Value) : null))
            .ToList();

        var stopsCoordinates = stops.Select(s => (s.Latitude, s.Longitude))
            .ToList();

        var warehouseCoordinates = (warehouse.Location.Latitude, warehouse.Location.Longitude);

        var matrix = await _distanceMatrixProvider.GetMatrixAsync(warehouseCoordinates, stopsCoordinates, ct);

        var input = new RouteOptimizerInput
        {
            Warehouse = warehouseCoordinates,
            Stops = stops,
            Matrix = matrix,
            RouteDate = request.RouteDate
        };

        if (!_optimizers.Any())
            throw new InvalidOperationException("No optimizers configured");

        var tasks = _optimizers.Select(o => o.OptimizeAsync(input, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);
        var best = results.MinBy(r => r.TotalDurationSeconds)!;

        var comparisons = results
            .Select(r => new AlgorithmComparison(r.AlgorithmName, r.TotalDurationSeconds, r.ComputationTimeMs))
            .ToList();

        var orderedStopIds = OrientTowardDepot(best.OrderedStopIds, stops, matrix);

        route.ApplyOptimizedOrders(orderedStopIds);
        route.Optimize();

        return new OptimizeRouteResult(route.Id, orderedStopIds, comparisons);
    }

    // The optimizers minimise a round trip, so the visiting direction is arbitrary.
    // Orient it so the route ends at the stop closest to the warehouse — the driver finishes near the depot.
    private static IReadOnlyList<Guid> OrientTowardDepot(IReadOnlyList<Guid> ordered, IReadOnlyList<StopInput> stops, DistanceMatrix matrix)
    {
        if (ordered.Count < 2)
            return ordered;

        var matrixIndexById = new Dictionary<Guid, int>(stops.Count);
        for (var i = 0; i < stops.Count; i++)
            matrixIndexById[stops[i].StopId] = i + 1;

        var depotToFirst = matrix.GetDuration(0, matrixIndexById[ordered[0]]);
        var depotToLast = matrix.GetDuration(0, matrixIndexById[ordered[^1]]);

        return depotToFirst < depotToLast ? ordered.Reverse().ToList() : ordered;
    }
}
