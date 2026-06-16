using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;

namespace RouteOptimizer.Application.Routes.GetRoutes;

public class GetRoutesQueryHandler : IRequestHandler<GetRoutesQuery, PagedResult<RouteListItemDto>>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUser _currentUser;

    public GetRoutesQueryHandler(
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

    public async Task<PagedResult<RouteListItemDto>> Handle(GetRoutesQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (routes, totalCount) = await _routeRepository.GetAllAsync(
            _currentUser.WarehouseId, request.Status, skip, request.PageSize, ct);

        var warehouses = await _warehouseRepository.GetAllAsync(ct);
        var warehouseNames = warehouses.ToDictionary(w => w.Id, w => w.Name);

        var shiftIds = routes
            .Where(r => r.AssignedShiftId.HasValue)
            .Select(r => r.AssignedShiftId!.Value)
            .Distinct();
        var shifts = await _shiftRepository.GetByIdsAsync(shiftIds, ct);
        var shiftsById = shifts.ToDictionary(s => s.Id);

        var driverIds = shifts.Select(s => s.DriverId).Distinct();
        var drivers = await _userRepository.GetByIdsAsync(driverIds, ct);
        var driverNames = drivers.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var items = routes.Select(r =>
        {
            DateOnly? shiftDate = null;
            string? driverName = null;

            if (r.AssignedShiftId.HasValue && shiftsById.TryGetValue(r.AssignedShiftId.Value, out var shift))
            {
                shiftDate = shift.ShiftDate;
                driverNames.TryGetValue(shift.DriverId, out driverName);
            }

            return new RouteListItemDto(
                r.Id,
                r.WarehouseId,
                r.Status.ToString(),
                r.Stops.Count,
                r.AssignedShiftId,
                shiftDate,
                driverName,
                warehouseNames.GetValueOrDefault(r.WarehouseId, string.Empty));
        }).ToList();

        return new PagedResult<RouteListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
