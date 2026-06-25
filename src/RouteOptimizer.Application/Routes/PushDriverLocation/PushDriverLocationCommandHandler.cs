using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.PushDriverLocation;

public class PushDriverLocationCommandHandler : IRequestHandler<PushDriverLocationCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IRouteEventsNotifier _notifier;

    public PushDriverLocationCommandHandler(ICurrentUser currentUser, IRouteRepository routeRepository, IDriverShiftRepository shiftRepository, IRouteEventsNotifier notifier)
    {
        _currentUser = currentUser;
        _routeRepository = routeRepository;
        _shiftRepository = shiftRepository;
        _notifier = notifier;
    }

    public async Task<Result> Handle(PushDriverLocationCommand request, CancellationToken ct)
    {
        if (_currentUser.WarehouseId is not { } warehouseId)
            return Result.Failure("No warehouse associated with this account");

        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (!await DriverRouteAccess.IsAssignedToDriverAsync(_shiftRepository, route, _currentUser.UserId, ct))
            return Result.Failure(DriverRouteAccess.AccessDeniedMessage);

        await _notifier.DriverLocationAsync(
            warehouseId, request.RouteId, _currentUser.UserId,
            request.Latitude, request.Longitude, DateTimeOffset.UtcNow, ct);

        return Result.Success();
    }
}
