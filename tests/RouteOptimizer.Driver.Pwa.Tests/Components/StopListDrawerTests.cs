using Bunit;
using FluentAssertions;
using RouteOptimizer.Driver.Pwa.Components;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Tests.Components;

public class StopListDrawerTests : Bunit.TestContext
{
    [Fact]
    public void RendersStopsOrderedBySequence()
    {
        var first = Stop(0, "Alpha St", "InProgress", "Anna");
        var second = Stop(1, "Beta St", "Pending", "Boris");

        var cut = RenderComponent<StopListDrawer>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Stops, new[] { second, first }));

        var addresses = cut.FindAll(".drawer__stop-address").Select(e => e.TextContent.Trim()).ToList();
        addresses.Should().ContainInOrder("Alpha St, City", "Beta St, City");
    }

    [Fact]
    public void Open_TogglesOpenModifierClass()
    {
        var cut = RenderComponent<StopListDrawer>(p => p
            .Add(c => c.Open, false)
            .Add(c => c.Stops, new[] { Stop(0, "Alpha St", "Pending", "Anna") }));

        cut.Find(".drawer").ClassList.Should().NotContain("drawer--open");

        cut.SetParametersAndRender(p => p.Add(c => c.Open, true));

        cut.Find(".drawer").ClassList.Should().Contain("drawer--open");
    }

    [Fact]
    public void HighlightsCurrentStop()
    {
        var current = Stop(0, "Alpha St", "InProgress", "Anna");
        var other = Stop(1, "Beta St", "Pending", "Boris");

        var cut = RenderComponent<StopListDrawer>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Stops, new[] { current, other })
            .Add(c => c.CurrentStopId, current.Id));

        cut.FindAll(".drawer__stop--current").Should().HaveCount(1);
    }

    [Fact]
    public void ClickingStop_InvokesOnSelectWithThatStop()
    {
        var target = Stop(1, "Beta St", "Pending", "Boris");
        RouteStop? selected = null;

        var cut = RenderComponent<StopListDrawer>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Stops, new[] { Stop(0, "Alpha St", "InProgress", "Anna"), target })
            .Add(c => c.OnSelect, (RouteStop s) => selected = s));

        cut.FindAll(".drawer__stop")[1].Click();

        selected.Should().NotBeNull();
        selected!.Id.Should().Be(target.Id);
    }

    [Fact]
    public void ClickingClose_InvokesOnClose()
    {
        var closed = false;

        var cut = RenderComponent<StopListDrawer>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Stops, new[] { Stop(0, "Alpha St", "InProgress", "Anna") })
            .Add(c => c.OnClose, () => closed = true));

        cut.Find(".drawer__close").Click();

        closed.Should().BeTrue();
    }

    private static RouteStop Stop(int sequence, string street, string status, string recipient)
    {
        var order = new StopOrder(Guid.NewGuid(), recipient, null, "Individual", "+10000000000", "InTransit");
        return new RouteStop(Guid.NewGuid(), sequence, "City", street, 52.0, 21.0, status,
            new[] { order.OrderId }, new[] { order });
    }
}
