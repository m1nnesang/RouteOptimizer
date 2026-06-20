using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.PushDriverLocation;

public class PushDriverLocationCommandHandler : IRequestHandler<PushDriverLocationCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRouteEventsNotifier _notifier;

    public PushDriverLocationCommandHandler(ICurrentUser currentUser, IRouteEventsNotifier notifier)
    {
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<Result> Handle(PushDriverLocationCommand request, CancellationToken ct)
    {
        if (_currentUser.WarehouseId is not { } warehouseId)
            return Result.Failure("No warehouse associated with this account");

        await _notifier.DriverLocationAsync(
            warehouseId, request.RouteId, _currentUser.UserId,
            request.Latitude, request.Longitude, DateTimeOffset.UtcNow, ct);

        return Result.Success();
    }
}
