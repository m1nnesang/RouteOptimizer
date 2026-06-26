using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Routes.PushDriverLocation;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Application.Tests.Routes;

public class PushDriverLocationCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();
    private readonly PushDriverLocationCommandHandler _handler;

    public PushDriverLocationCommandHandlerTests()
    {
        _handler = new PushDriverLocationCommandHandler(_currentUser.Object, _routeRepository.Object, _shiftRepository.Object, _notifier.Object);
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

        var route = CreateAssignedRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _shiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverShift.Create(driverId, Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow)).Value!);

        var result = await _handler.Handle(new PushDriverLocationCommand(routeId, 52.1, 21.0), default);

        result.IsSuccess.Should().BeTrue();
        _notifier.Verify(x => x.DriverLocationAsync(
            warehouseId, routeId, driverId, 52.1, 21.0, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RouteNotOwnedByDriver_ReturnsFailureAndDoesNotBroadcast()
    {
        _currentUser.SetupGet(x => x.WarehouseId).Returns(Guid.NewGuid());
        _currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var route = CreateAssignedRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _shiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverShift.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow)).Value!);

        var result = await _handler.Handle(new PushDriverLocationCommand(Guid.NewGuid(), 52.1, 21.0), default);

        result.IsFailure.Should().BeTrue();
        _notifier.Verify(x => x.DriverLocationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Route CreateAssignedRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        return route;
    }
}
