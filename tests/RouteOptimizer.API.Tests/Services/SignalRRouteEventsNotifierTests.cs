using Microsoft.AspNetCore.SignalR;
using Moq;
using RouteOptimizer.API.Hubs;
using RouteOptimizer.API.Services;

namespace RouteOptimizer.API.Tests.Services;

public class SignalRRouteEventsNotifierTests
{
    private readonly Mock<IHubClients> _clients = new();
    private readonly Mock<IClientProxy> _group = new();
    private readonly SignalRRouteEventsNotifier _notifier;
    private readonly Guid _warehouseId = Guid.NewGuid();

    public SignalRRouteEventsNotifierTests()
    {
        var hubContext = new Mock<IHubContext<RouteHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(_clients.Object);
        _clients.Setup(c => c.Group(RouteHub.WarehouseGroup(_warehouseId))).Returns(_group.Object);
        _notifier = new SignalRRouteEventsNotifier(hubContext.Object);
    }

    [Fact]
    public async Task RouteStartedAsync_SendsToWarehouseGroup()
    {
        await _notifier.RouteStartedAsync(_warehouseId, Guid.NewGuid(), default);

        VerifySent(SignalRRouteEventsNotifier.RouteStartedEvent);
    }

    [Fact]
    public async Task StopCompletedAsync_SendsToWarehouseGroup()
    {
        await _notifier.StopCompletedAsync(_warehouseId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default);

        VerifySent(SignalRRouteEventsNotifier.StopCompletedEvent);
    }

    [Fact]
    public async Task StopFailedAsync_SendsToWarehouseGroup()
    {
        await _notifier.StopFailedAsync(_warehouseId, Guid.NewGuid(), Guid.NewGuid(), null, default);

        VerifySent(SignalRRouteEventsNotifier.StopFailedEvent);
    }

    [Fact]
    public async Task StopSkippedAsync_SendsToWarehouseGroup()
    {
        await _notifier.StopSkippedAsync(_warehouseId, Guid.NewGuid(), Guid.NewGuid(), null, default);

        VerifySent(SignalRRouteEventsNotifier.StopSkippedEvent);
    }

    private void VerifySent(string eventName) =>
        _group.Verify(g => g.SendCoreAsync(
            eventName,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
}
