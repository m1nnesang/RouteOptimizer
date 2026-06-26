using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteEntity = RouteOptimizer.Domain.Entities.Route.Route;

namespace RouteOptimizer.Application.Routes;

public static class DriverRouteAccess
{
    public const string AccessDeniedMessage = "Route is not assigned to you";

    public static async Task<bool> IsAssignedToDriverAsync(
        IDriverShiftRepository shiftRepository,
        RouteEntity route,
        Guid driverId,
        CancellationToken ct)
    {
        if (route.AssignedShiftId is null)
            return false;

        var shift = await shiftRepository.GetByIdAsync(route.AssignedShiftId.Value, ct);

        return shift is not null && shift.DriverId == driverId;
    }

    public static async Task<Result> EnsureAssignedToDriverAsync(
        IDriverShiftRepository shiftRepository,
        RouteEntity route,
        Guid driverId,
        CancellationToken ct)
    {
        var owns = await IsAssignedToDriverAsync(shiftRepository, route, driverId, ct);

        return owns ? Result.Success() : Result.Failure(AccessDeniedMessage);
    }
}
