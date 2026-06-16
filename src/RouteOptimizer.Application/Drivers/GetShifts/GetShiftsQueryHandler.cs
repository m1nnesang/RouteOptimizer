using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;

namespace RouteOptimizer.Application.Drivers.GetShifts;

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, PagedResult<ShiftListItemDto>>
{
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUser _currentUser;

    public GetShiftsQueryHandler(
        IDriverShiftRepository shiftRepository,
        IUserRepository userRepository,
        IVehicleRepository vehicleRepository,
        IWarehouseRepository warehouseRepository,
        ICurrentUser currentUser)
    {
        _shiftRepository = shiftRepository;
        _userRepository = userRepository;
        _vehicleRepository = vehicleRepository;
        _warehouseRepository = warehouseRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ShiftListItemDto>> Handle(GetShiftsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (shifts, totalCount) = await _shiftRepository.GetAllAsync(
            _currentUser.WarehouseId, request.Date, skip, request.PageSize, ct);

        // Awaited sequentially: the repositories share one scoped AppDbContext,
        // which does not support concurrent operations.
        var drivers = await _userRepository.GetByIdsAsync(shifts.Select(s => s.DriverId).Distinct(), ct);
        var vehicles = await _vehicleRepository.GetByIdsAsync(shifts.Select(s => s.VehicleId).Distinct(), ct);
        var warehouses = await _warehouseRepository.GetAllAsync(ct);

        var driverNames = drivers.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        var vehicleTypes = vehicles.ToDictionary(v => v.Id, v => v.Type);
        var warehouseNames = warehouses.ToDictionary(w => w.Id, w => w.Name);

        var items = shifts
            .Select(s => new ShiftListItemDto(
                s.Id,
                s.DriverId,
                s.VehicleId,
                s.WarehouseId,
                s.ShiftDate,
                s.StartedAt,
                s.EndedAt,
                driverNames.GetValueOrDefault(s.DriverId, string.Empty),
                vehicleTypes.GetValueOrDefault(s.VehicleId, string.Empty),
                warehouseNames.GetValueOrDefault(s.WarehouseId, string.Empty)))
            .ToList();

        return new PagedResult<ShiftListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
