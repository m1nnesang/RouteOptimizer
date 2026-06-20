using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Routes.PushDriverLocation;

namespace RouteOptimizer.Application.Tests.Routes;

public class PushDriverLocationCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();
    private readonly PushDriverLocationCommandHandler _handler;

    public PushDriverLocationCommandHandlerTests()
    {
        _handler = new PushDriverLocationCommandHandler(_currentUser.Object, _notifier.Object);
    }

    [Fact]
    public async Task Handle_NoWarehouse_ReturnsFailureAndDoesNotBroadcast()
    {
        _currentUser.SetupGet(x => x.WarehouseId).Returns((Guid?)null);

        var result = await _handler.Handle(new PushDriverLocationCommand(Guid.NewGuid(), 52.1, 21.0), default);

        result.IsFailure.Should().BeTrue();
        _notifier.Verify(x => x.DriverLocationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithWarehouse_BroadcastsToWarehouseGroup()
    {
        var warehouseId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        _currentUser.SetupGet(x => x.WarehouseId).Returns(warehouseId);
        _currentUser.SetupGet(x => x.UserId).Returns(driverId);

        var result = await _handler.Handle(new PushDriverLocationCommand(routeId, 52.1, 21.0), default);

        result.IsSuccess.Should().BeTrue();
        _notifier.Verify(x => x.DriverLocationAsync(
            warehouseId, routeId, driverId, 52.1, 21.0, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
