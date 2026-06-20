using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Orders;

public static class StopResolution
{
    public static void ResolveIfComplete(Stop stop, IReadOnlyList<Order> stopOrders)
    {
        if (stopOrders.Count == 0)
            return;

        var pending = stopOrders.Any(o => o.Status is OrderStatus.InTransit or OrderStatus.AssignedToRoute);

        if (pending)
            return;

        var delivered = stopOrders.Count(o => o.Status == OrderStatus.Delivered);
        var failed = stopOrders.Count(o => o.Status == OrderStatus.Failed);

        if (failed == 0)
            stop.Complete();
        else if (delivered == 0)
            stop.Failed();
        else
            stop.PartiallyComplete();
    }
}
