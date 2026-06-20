using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Routes.GetRoutes;

namespace RouteOptimizer.Application.Routes.GetMyRoutes;

public class GetMyRoutesQueryHandler : IRequestHandler<GetMyRoutesQuery, IReadOnlyList<RouteListItemDto>>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyRoutesQueryHandler(
        IRouteRepository routeRepository,
        IDriverShiftRepository shiftRepository,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _shiftRepository = shiftRepository;
        _userRepository = userRepository;
        _warehouseRepository = warehouseRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RouteListItemDto>> Handle(GetMyRoutesQuery request, CancellationToken ct)
    {
        var shift = await _shiftRepository.GetActiveShiftByDriverIdAsync(_currentUser.UserId, ct);

        if (shift is null)
            return [];

        var routes = await _routeRepository.GetByAssignedShiftIdAsync(shift.Id, ct);

        if (routes.Count == 0)
            return [];

        var warehouses = await _warehouseRepository.GetAllAsync(ct);
        var warehouseNames = warehouses.ToDictionary(w => w.Id, w => w.Name);

        var driver = await _userRepository.GetByIdAsync(_currentUser.UserId, ct);
        var driverName = driver is null ? null : $"{driver.FirstName} {driver.LastName}";

        return routes.Select(r => new RouteListItemDto(
            r.Id,
            r.WarehouseId,
            r.Status.ToString(),
            r.Stops.Count,
            r.AssignedShiftId,
            r.Date,
            driverName,
            warehouseNames.GetValueOrDefault(r.WarehouseId, string.Empty))).ToList();
    }
}
