using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RouteOptimizer.API.Hubs;

[Authorize]
public class RouteHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public Task SendRouteUpdate(Guid routeId, string message)
    {
        return Clients.All.SendAsync("RouteUpdated", routeId, message);
    }
}
