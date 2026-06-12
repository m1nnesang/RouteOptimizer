using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Stops.FailDelivery;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class FailDeliveryHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverShiftRepository> _driverShiftRepository = new();
    private readonly Mock<IDeliveryAttemptRepository> _deliveryAttemptRepository = new();
    private readonly FailDeliveryHandler _handler;

    public FailDeliveryHandlerTests()
    {
        _handler = new FailDeliveryHandler(
            _routeRepository.Object,
            _driverShiftRepository.Object,
            _deliveryAttemptRepository.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(ValidCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RouteNotInProgress_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(ValidCommand(route.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StopNotFound_ThrowsNotFoundException()
    {
        var route = CreateInProgressRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var act = () => _handler.Handle(ValidCommand(route.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_StopNotInProgress_ReturnsFailure()
    {
        var route = CreateInProgressRoute();
        var stop = CreatePendingStop(route);
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not in progress");
    }

    [Fact]
    public async Task Handle_ValidRequest_StopBecomesFailed()
    {
        var driverId = Guid.NewGuid();
        var (route, stop) = CreateRouteWithInProgressStop([Guid.NewGuid()]);
        SetupRepositories(route, driverId);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id, driverId), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.Failed);
    }

    [Fact]
    public async Task Handle_StopWithTwoOrders_CreatesDeliveryAttemptForEachOrder()
    {
        var driverId = Guid.NewGuid();
        var orderIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var (route, stop) = CreateRouteWithInProgressStop(orderIds);
        SetupRepositories(route, driverId);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id, driverId), default);

        result.IsSuccess.Should().BeTrue();
        _deliveryAttemptRepository.Verify(
            x => x.AddAsync(It.IsAny<DeliveryAttempt>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_FailureReasonOtherWithNotes_ReturnsSuccess()
    {
        var driverId = Guid.NewGuid();
        var (route, stop) = CreateRouteWithInProgressStop([Guid.NewGuid()]);
        SetupRepositories(route, driverId);

        var command = new FailDeliveryCommand(
            driverId, route.Id, stop.Id, 52.2297, 21.0122,
            FailureReason.Other, "Door code not working", null);

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
    }

    private void SetupRepositories(Route route, Guid driverId)
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var shift = DriverShift.Create(
            driverId, Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))).Value!;

        _driverShiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        _deliveryAttemptRepository
            .Setup(x => x.AddAsync(It.IsAny<DeliveryAttempt>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static (Route route, Stop stop) CreateRouteWithInProgressStop(IEnumerable<Guid> orderIds)
    {
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route, orderIds.ToList());
        return (route, stop);
    }

    private static Route CreateInProgressRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        return route;
    }

    private static Stop CreatePendingStop(Route route, IReadOnlyList<Guid>? orders = null)
    {
        var address = Address.Create("ul. Marszalkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, orders ?? []).Value!;
        route.AddStop(stop);
        return stop;
    }

    private static Stop CreateInProgressStop(Route route, IReadOnlyList<Guid>? orders = null)
    {
        var stop = CreatePendingStop(route, orders);
        stop.Start();
        return stop;
    }

    private static FailDeliveryCommand ValidCommand(Guid routeId, Guid stopId, Guid? driverId = null) =>
        new(driverId ?? Guid.NewGuid(), routeId, stopId, 52.2297, 21.0122, FailureReason.CustomerNotAtHome, null, null);
}
