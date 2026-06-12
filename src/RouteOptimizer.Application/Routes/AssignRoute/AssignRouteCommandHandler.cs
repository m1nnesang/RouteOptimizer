using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.AssignRoute;

public class AssignRouteCommandHandler : IRequestHandler<AssignRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _driverShiftRepository;

    public AssignRouteCommandHandler(IRouteRepository routeRepository, IDriverShiftRepository driverShiftRepository)
    {
        _routeRepository = routeRepository;
        _driverShiftRepository = driverShiftRepository;
    }

    public async Task<Result> Handle(AssignRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        var shift = await _driverShiftRepository.GetByIdAsync(request.ShiftId, ct);

        if (shift is null)
            throw new NotFoundException("Shift not found");

        if (shift.WarehouseId != route.WarehouseId)
            return Result.Failure("Shift warehouse does not match route warehouse");

        try
        {
            route.AssignShift(request.ShiftId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
