using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Common;

public static class OfflineRouteMutator
{
    public static RouteDetail DeliverOrder(RouteDetail route, Guid stopId, Guid orderId) =>
        ResolveAndAdvance(SetOrderStatus(route, stopId, orderId, "Delivered"), stopId);

    public static RouteDetail FailOrder(RouteDetail route, Guid stopId, Guid orderId) =>
        ResolveAndAdvance(SetOrderStatus(route, stopId, orderId, "Failed"), stopId);

    public static RouteDetail SkipStop(RouteDetail route, Guid stopId) =>
        Advance(SetStopStatus(route, stopId, "Skipped"));

    public static RouteDetail ResumeStop(RouteDetail route, Guid stopId) =>
        SetStopStatus(route, stopId, "InProgress");

    public static RouteDetail StartRoute(RouteDetail route)
    {
        var started = route with { Status = "InProgress" };
        var first = started.Stops.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Status == "Pending");

        return first is null ? started : ActivateStop(started, first.Id);
    }

    public static RouteDetail CompleteRoute(RouteDetail route) => route with { Status = "Completed" };

    private static RouteDetail SetOrderStatus(RouteDetail route, Guid stopId, Guid orderId, string status) =>
        MapStop(route, stopId, stop => stop with
        {
            Orders = stop.Orders
                .Select(o => o.OrderId == orderId ? o with { Status = status } : o)
                .ToList()
        });

    private static RouteDetail SetStopStatus(RouteDetail route, Guid stopId, string status) =>
        MapStop(route, stopId, stop => stop with { Status = status });

    private static RouteDetail ResolveAndAdvance(RouteDetail route, Guid stopId)
    {
        var stop = route.Stops.FirstOrDefault(s => s.Id == stopId);

        if (stop is null || stop.Status != "InProgress" || stop.Orders.Count == 0)
            return route;

        var open = stop.Orders.Any(o => o.Status is "InTransit" or "AssignedToRoute" or "Created");

        if (open)
            return route;

        var failed = stop.Orders.Count(o => o.Status == "Failed");
        var delivered = stop.Orders.Count(o => o.Status == "Delivered");

        var resolved = failed == 0 ? "Completed" : delivered == 0 ? "Failed" : "PartiallyCompleted";

        return Advance(SetStopStatus(route, stopId, resolved));
    }

    private static RouteDetail Advance(RouteDetail route)
    {
        var next = route.Stops.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Status == "Pending");

        return next is null ? route : ActivateStop(route, next.Id);
    }

    private static RouteDetail ActivateStop(RouteDetail route, Guid stopId) =>
        MapStop(route, stopId, stop => stop with
        {
            Status = "InProgress",
            Orders = stop.Orders
                .Select(o => o.Status is "AssignedToRoute" or "Created" ? o with { Status = "InTransit" } : o)
                .ToList()
        });

    private static RouteDetail MapStop(RouteDetail route, Guid stopId, Func<RouteStop, RouteStop> map) =>
        route with
        {
            Stops = route.Stops.Select(s => s.Id == stopId ? map(s) : s).ToList()
        };
}
