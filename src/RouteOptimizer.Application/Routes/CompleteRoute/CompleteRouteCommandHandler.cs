using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.CompleteRoute;

public class CompleteRouteCommandHandler : IRequestHandler<CompleteRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly ICurrentUser _currentUser;

    public CompleteRouteCommandHandler(IRouteRepository routeRepository, IDriverShiftRepository shiftRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _shiftRepository = shiftRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CompleteRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route not found");

        if (route.Stops.Any(s => s.Status is StopStatus.Pending or StopStatus.InProgress))
            return Result.Failure("Route still has incomplete stops");

        try
        {
            route.Complete();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await EndAssignedShiftAsync(route.AssignedShiftId, ct);

        return Result.Success();
    }

    private async Task EndAssignedShiftAsync(Guid? shiftId, CancellationToken ct)
    {
        if (shiftId is not { } id)
            return;

        var shift = await _shiftRepository.GetByIdAsync(id, ct);

        if (shift is { StartedAt: not null, EndedAt: null })
            shift.End();
    }
}
