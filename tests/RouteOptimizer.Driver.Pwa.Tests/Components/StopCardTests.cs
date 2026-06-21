using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RouteOptimizer.Driver.Pwa.Components;
using RouteOptimizer.Driver.Pwa.Models;
using RouteOptimizer.Driver.Pwa.Services;

namespace RouteOptimizer.Driver.Pwa.Tests.Components;

public class StopCardTests : Bunit.TestContext
{
    private readonly Mock<IRouteApi> _routeApi = new();
    private readonly Mock<IGeolocation> _geolocation = new();
    private readonly Mock<IConnectivity> _connectivity = new();

    public StopCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_routeApi.Object);
        Services.AddSingleton(_geolocation.Object);
        Services.AddSingleton(_connectivity.Object);
    }

    [Fact]
    public void ActiveStop_RendersDeliverAndFailButtons()
    {
        var cut = RenderCard(Stop("InProgress", "InTransit"), routeInProgress: true);

        cut.FindAll("button.btn--success").Should().ContainSingle();
        cut.FindAll("button.btn--danger").Should().ContainSingle();
    }

    [Fact]
    public void RouteNotInProgress_HidesActionButtons()
    {
        var cut = RenderCard(Stop("InProgress", "InTransit"), routeInProgress: false);

        cut.FindAll("button.btn--success").Should().BeEmpty();
        cut.FindAll("button.stop-card__skip").Should().BeEmpty();
    }

    [Fact]
    public void ClosedOrder_ShowsBadgeWithoutActions()
    {
        var cut = RenderCard(Stop("InProgress", "Delivered"), routeInProgress: true);

        cut.FindAll("button.btn--success").Should().BeEmpty();
        cut.Find(".order-row .badge").TextContent.Should().Be("Доставлен");
    }

    [Fact]
    public void ClickingDeliver_CallsApiAndRaisesOnChanged()
    {
        var stop = Stop("InProgress", "InTransit");
        var order = stop.Orders[0];
        _routeApi.Setup(a => a.DeliverOrderAsync(It.IsAny<Guid>(), stop.Id, order.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Ok);
        var changed = false;

        var cut = RenderCard(stop, routeInProgress: true, () => changed = true);
        cut.Find("button.btn--success").Click();

        _routeApi.Verify(a => a.DeliverOrderAsync(It.IsAny<Guid>(), stop.Id, order.OrderId, It.IsAny<CancellationToken>()), Times.Once);
        changed.Should().BeTrue();
    }

    [Fact]
    public void DeliverFails_ShowsErrorAndDoesNotRaiseOnChanged()
    {
        var stop = Stop("InProgress", "InTransit");
        _routeApi.Setup(a => a.DeliverOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Fail("Маршрут завершён"));
        var changed = false;

        var cut = RenderCard(stop, routeInProgress: true, () => changed = true);
        cut.Find("button.btn--success").Click();

        cut.Find(".action-error").TextContent.Should().Be("Маршрут завершён");
        changed.Should().BeFalse();
    }

    [Fact]
    public void SkippedStop_ShowsResumeButton_ThatCallsApi()
    {
        var stop = Stop("Skipped", "InTransit");
        _routeApi.Setup(a => a.ResumeStopAsync(It.IsAny<Guid>(), stop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Ok);

        var cut = RenderCard(stop, routeInProgress: true);
        var resume = cut.Find("button.stop-card__skip");
        resume.TextContent.Trim().Should().Be("Вернуться к стопу");

        resume.Click();

        _routeApi.Verify(a => a.ResumeStopAsync(It.IsAny<Guid>(), stop.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private IRenderedComponent<StopCard> RenderCard(RouteStop stop, bool routeInProgress, Action? onChanged = null) =>
        RenderComponent<StopCard>(p => p
            .Add(c => c.Stop, stop)
            .Add(c => c.RouteId, Guid.NewGuid())
            .Add(c => c.RouteInProgress, routeInProgress)
            .Add(c => c.OnChanged, () => onChanged?.Invoke()));

    private static RouteStop Stop(string stopStatus, string orderStatus)
    {
        var order = new StopOrder(Guid.NewGuid(), "Recipient", null, "Individual", "+10000000000", orderStatus);
        return new RouteStop(Guid.NewGuid(), 0, "City", "Street", 52.0, 21.0, stopStatus,
            new[] { order.OrderId }, new[] { order });
    }
}
