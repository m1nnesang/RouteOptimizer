using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Stops.StartStop;

public class StartStopHandler : IRequestHandler<StartStopCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly ICurrentUser _currentUser;

    public StartStopHandler(IRouteRepository routeRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(StartStopCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route is not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route is not found");

        if (route.Status != RouteStatus.InProgress)
            return Result.Failure("Route is not in progress");

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId);

        if (stop is null)
            throw new NotFoundException("Stop is not found");

        if (stop.Status != StopStatus.Pending)
            return Result.Failure("Stop cannot be started");

        try
        {
            stop.Start();
        }

        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
