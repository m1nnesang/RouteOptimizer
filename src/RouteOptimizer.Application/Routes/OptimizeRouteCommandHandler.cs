using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Routes;

public class OptimizeRouteCommandHandler : IRequestHandler<OptimizeRouteCommand, OptimizeRouteResult>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IDistanceMatrixProvider _distanceMatrixProvider;
    private readonly IEnumerable<IRouteOptimizer> _optimizers;
    private readonly IUnitOfWork _unitOfWork;

    public OptimizeRouteCommandHandler(IRouteRepository routeRepository, IWarehouseRepository warehouseRepository, IDistanceMatrixProvider distanceMatrixProvider, IEnumerable<IRouteOptimizer> optimizers,IUnitOfWork unitOfWork)
    {
        (_routeRepository, _warehouseRepository, _distanceMatrixProvider, _optimizers, _unitOfWork) = (routeRepository, warehouseRepository, distanceMatrixProvider, optimizers, unitOfWork);
    }

    public async Task<OptimizeRouteResult> Handle(OptimizeRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        var warehouse = await _warehouseRepository.GetByIdAsync(route.WarehouseId, ct);

        if (warehouse is null)
            throw new NotFoundException("Warehouse not found");

        var stops = route.Stops.Select(s => new StopInput(s.Id, s.Location.Latitude, s.Location.Longitude,
            s.DeliveryWindow is {Start:not null, End:not null,} dw? new TimeWindow(dw.Start.Value, dw.End.Value) : null))
            .ToList();

        var stopsCoordinates = route.Stops.Select(s => (s.Location.Latitude, s.Location.Longitude) )
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

        var tasks = _optimizers.Select(o => o.OptimizeAsync(input, ct));

        if (!_optimizers.Any())
            throw new InvalidOperationException("No optimizers configured");
        var results = await Task.WhenAll(tasks);
        var best = results.MinBy(r => r.TotalDurationSeconds)!;

        var comparisons = results
            .Select(r => new AlgorithmComparison(r.AlgorithmName, r.TotalDurationSeconds))
            .ToList();

        route.ApplyOptimizedOrders(best.OrderedStopIds);
        route.Optimize();

        await _unitOfWork.SaveChangesAsync(ct);

        return new OptimizeRouteResult(route.Id, best.OrderedStopIds, comparisons);
    }
}
