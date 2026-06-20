using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Orders.FailOrder;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class FailOrderHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IDriverShiftRepository> _driverShiftRepository = new();
    private readonly Mock<IDeliveryAttemptRepository> _deliveryAttemptRepository = new();
    private readonly FailOrderHandler _handler;

    public FailOrderHandlerTests()
    {
        _handler = new FailOrderHandler(
            _routeRepository.Object,
            _orderRepository.Object,
            _driverShiftRepository.Object,
            _deliveryAttemptRepository.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(ValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DriverMismatch_ReturnsFailure()
    {
        var order = CreateInTransitOrder();
        var (route, stop) = CreateRouteWithStop([order]);
        SetupRepositories(route, stop, Guid.NewGuid(), order);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id, order.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesSingleDeliveryAttemptAndFailsOrder()
    {
        var driverId = Guid.NewGuid();
        var order = CreateInTransitOrder();
        var (route, stop) = CreateRouteWithStop([order]);
        SetupRepositories(route, stop, driverId, order);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id, order.Id, driverId), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Failed);
        _deliveryAttemptRepository.Verify(
            x => x.AddAsync(It.IsAny<DeliveryAttempt>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LastOrderFailed_StopBecomesFailed()
    {
        var driverId = Guid.NewGuid();
        var order = CreateInTransitOrder();
        var (route, stop) = CreateRouteWithStop([order]);
        SetupRepositories(route, stop, driverId, order);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id, order.Id, driverId), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.Failed);
    }

    [Fact]
    public async Task Handle_OneFailedOneAlreadyDelivered_StopBecomesPartiallyCompleted()
    {
        var driverId = Guid.NewGuid();
        var delivered = CreateInTransitOrder();
        delivered.MarkAsDelivered();
        var failing = CreateInTransitOrder();
        var (route, stop) = CreateRouteWithStop([delivered, failing]);
        SetupRepositories(route, stop, driverId, delivered, failing);

        var result = await _handler.Handle(ValidCommand(route.Id, stop.Id, failing.Id, driverId), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.PartiallyCompleted);
    }

    private void SetupRepositories(Route route, Stop stop, Guid driverId, params Order[] orders)
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        _orderRepository
            .Setup(x => x.GetByIdsAsync(stop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

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

    private static Order CreateInTransitOrder()
    {
        var address = Address.Create("ul. Marszalkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var weight = Weight.Create(10).Value!;
        var volume = Volume.Create(5).Value!;
        var phone = PhoneNumber.Create("+48601234567").Value!;
        var order = IndividualOrder.Create(Guid.NewGuid(), address, location, weight, volume, DeliveryWindow.AnyTime(),
            phone, CargoType.General, null, "Jan Kowalski", true).Value!;

        order.AssignToRoute(Guid.NewGuid());
        order.MarkAsInTransit();
        return order;
    }

    private static (Route route, Stop stop) CreateRouteWithStop(IReadOnlyList<Order> orders)
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();

        var address = Address.Create("ul. Marszalkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, orders.Select(o => o.Id).ToList()).Value!;
        route.AddStop(stop);
        stop.Start();
        return (route, stop);
    }

    private static FailOrderCommand ValidCommand(Guid routeId, Guid stopId, Guid orderId, Guid? driverId = null) =>
        new(driverId ?? Guid.NewGuid(), routeId, stopId, orderId, 52.2297, 21.0122,
            FailureReason.CustomerNotAtHome, null, null);
}
