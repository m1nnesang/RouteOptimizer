using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.API.Hubs;

[Authorize]
public class RouteHub : Hub
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;

    public RouteHub(IRouteRepository routeRepository, IDriverShiftRepository shiftRepository)
    {
        _routeRepository = routeRepository;
        _shiftRepository = shiftRepository;
    }

    public static string WarehouseGroup(Guid warehouseId) => $"warehouse-{warehouseId}";
    public static string RouteGroup(Guid routeId) => $"route-{routeId}";

    [Authorize(Roles = "Dispatcher")]
    public async Task JoinWarehouse()
    {
        var warehouseClaim = Context.User?.FindFirst("warehouse_id")?.Value;

        if (!Guid.TryParse(warehouseClaim, out var warehouseId))
            throw new HubException("No warehouse associated with this account");

        await Groups.AddToGroupAsync(Context.ConnectionId, WarehouseGroup(warehouseId));
    }

    [Authorize(Roles = "Driver")]
    public async Task JoinRoute(Guid routeId)
    {
        var ct = Context.ConnectionAborted;

        if (!Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var driverId))
            throw new HubException("No driver associated with this account");

        var shift = await _shiftRepository.GetActiveShiftByDriverIdAsync(driverId, ct);
        var route = await _routeRepository.GetByIdAsync(routeId, ct);

        if (shift is null || route is null || route.AssignedShiftId != shift.Id)
            throw new HubException("Route is not assigned to your active shift");

        await Groups.AddToGroupAsync(Context.ConnectionId, RouteGroup(routeId), ct);
    }
}
