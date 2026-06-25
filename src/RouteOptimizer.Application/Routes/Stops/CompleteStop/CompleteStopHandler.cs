using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Stops.CompleteStop;

public class CompleteStopHandler : IRequestHandler<CompleteStopCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly ICurrentUser _currentUser;

    public CompleteStopHandler(IRouteRepository routeRepository, IDriverShiftRepository shiftRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _shiftRepository = shiftRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CompleteStopCommand request, CancellationToken ct)
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

        if (request.IsPartial)
            stop.PartiallyComplete();

        else
            try
            {

                stop.Complete();
            }

            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }

        return Result.Success();
    }
}
