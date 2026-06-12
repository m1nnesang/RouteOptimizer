using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RouteOptimizer.API.Hubs;

[Authorize]
public class RouteHub : Hub
{
    public static string WarehouseGroup(Guid warehouseId) => $"warehouse-{warehouseId}";

    [Authorize(Roles = "Dispatcher")]
    public async Task JoinWarehouse()
    {
        var warehouseClaim = Context.User?.FindFirst("warehouse_id")?.Value;

        if (!Guid.TryParse(warehouseClaim, out var warehouseId))
            throw new HubException("No warehouse associated with this account");

        await Groups.AddToGroupAsync(Context.ConnectionId, WarehouseGroup(warehouseId));
    }
}
