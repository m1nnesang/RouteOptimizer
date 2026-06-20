using FluentAssertions;
using RouteOptimizer.Driver.Pwa.Common;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Tests;

public class OfflineRouteMutatorTests
{
    [Fact]
    public void DeliverOrder_LastOrder_CompletesStopAndAdvances()
    {
        var o1 = Order("InTransit");
        var o2 = Order("InTransit");
        var stop1 = Stop(0, "InProgress", o1);
        var stop2 = Stop(1, "Pending", o2);
        var route = Route("InProgress", stop1, stop2);

        var result = OfflineRouteMutator.DeliverOrder(route, stop1.Id, o1.OrderId);

        result.Stops.Single(s => s.Id == stop1.Id).Status.Should().Be("Completed");
        var advanced = result.Stops.Single(s => s.Id == stop2.Id);
        advanced.Status.Should().Be("InProgress");
        advanced.Orders.Single().Status.Should().Be("InTransit");
    }

    [Fact]
    public void DeliverOrder_OneOfTwo_StopStaysInProgress()
    {
        var o1 = Order("InTransit");
        var o2 = Order("InTransit");
        var stop = Stop(0, "InProgress", o1, o2);
        var route = Route("InProgress", stop);

        var result = OfflineRouteMutator.DeliverOrder(route, stop.Id, o1.OrderId);

        var updated = result.Stops.Single();
        updated.Status.Should().Be("InProgress");
        updated.Orders.Single(o => o.OrderId == o1.OrderId).Status.Should().Be("Delivered");
        updated.Orders.Single(o => o.OrderId == o2.OrderId).Status.Should().Be("InTransit");
    }

    [Fact]
    public void DeliverOrder_LastWithEarlierFailure_PartiallyCompleted()
    {
        var delivered = Order("InTransit");
        var failed = Order("Failed");
        var stop = Stop(0, "InProgress", delivered, failed);
        var route = Route("InProgress", stop);

        var result = OfflineRouteMutator.DeliverOrder(route, stop.Id, delivered.OrderId);

        result.Stops.Single().Status.Should().Be("PartiallyCompleted");
    }

    [Fact]
    public void FailOrder_OnlyOrder_StopFailedAndAdvances()
    {
        var o1 = Order("InTransit");
        var o2 = Order("AssignedToRoute");
        var stop1 = Stop(0, "InProgress", o1);
        var stop2 = Stop(1, "Pending", o2);
        var route = Route("InProgress", stop1, stop2);

        var result = OfflineRouteMutator.FailOrder(route, stop1.Id, o1.OrderId);

        result.Stops.Single(s => s.Id == stop1.Id).Status.Should().Be("Failed");
        result.Stops.Single(s => s.Id == stop2.Id).Status.Should().Be("InProgress");
    }

    [Fact]
    public void SkipStop_AdvancesToNextPending()
    {
        var stop1 = Stop(0, "InProgress", Order("InTransit"));
        var stop2 = Stop(1, "Pending", Order("AssignedToRoute"));
        var route = Route("InProgress", stop1, stop2);

        var result = OfflineRouteMutator.SkipStop(route, stop1.Id);

        result.Stops.Single(s => s.Id == stop1.Id).Status.Should().Be("Skipped");
        result.Stops.Single(s => s.Id == stop2.Id).Status.Should().Be("InProgress");
    }

    [Fact]
    public void StartRoute_ActivatesFirstPendingStop()
    {
        var stop1 = Stop(0, "Pending", Order("AssignedToRoute"));
        var stop2 = Stop(1, "Pending", Order("AssignedToRoute"));
        var route = Route("Assigned", stop1, stop2);

        var result = OfflineRouteMutator.StartRoute(route);

        result.Status.Should().Be("InProgress");
        result.Stops.Single(s => s.Id == stop1.Id).Status.Should().Be("InProgress");
        result.Stops.Single(s => s.Id == stop1.Id).Orders.Single().Status.Should().Be("InTransit");
        result.Stops.Single(s => s.Id == stop2.Id).Status.Should().Be("Pending");
    }

    private static StopOrder Order(string status) =>
        new(Guid.NewGuid(), "Recipient", null, "Individual", "+10000000000", status);

    private static RouteStop Stop(int sequence, string status, params StopOrder[] orders) =>
        new(Guid.NewGuid(), sequence, "City", "Street", 52.0, 21.0, status,
            orders.Select(o => o.OrderId).ToList(), orders.ToList());

    private static RouteDetail Route(string status, params RouteStop[] stops) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), status, stops.ToList());
}
