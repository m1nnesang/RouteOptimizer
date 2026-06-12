using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Orders.CancelOrder;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Orders;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _handler = new CancelOrderCommandHandler(_orderRepository.Object, _routeRepository.Object);
    }

    #region Tests

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = new CancelOrderCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_OrderWithDeliveredStatus_ReturnsFailure()
    {
        var order = CreateIndividualOrder();
        order.AssignToRoute(Guid.NewGuid());
        order.MarkAsInTransit();
        order.MarkAsDelivered();

        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new CancelOrderCommand(order.Id);
        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Order cannot be cancelled");
    }

    [Fact]
    public async Task Handle_CreatedOrder_ReturnsSuccess()
    {
        var order = CreateIndividualOrder();

        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new CancelOrderCommand(order.Id);
        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AssignedOrder_RemovesOrderFromStop()
    {
        var order = CreateIndividualOrder();
        var route = Route.Create(Guid.NewGuid()).Value!;
        var otherOrderId = Guid.NewGuid();
        var stop = AddPendingStop(route, [order.Id, otherOrderId], 0);
        order.AssignToRoute(route.Id);

        SetupRepositories(order, route);

        var result = await _handler.Handle(new CancelOrderCommand(order.Id), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.AssignedRouteId.Should().BeNull();
        stop.Orders.Should().ContainSingle().Which.Should().Be(otherOrderId);
        route.Stops.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_LastOrderOnStop_RemovesStopAndResequences()
    {
        var order = CreateIndividualOrder();
        var route = Route.Create(Guid.NewGuid()).Value!;
        AddPendingStop(route, [order.Id], 0);
        var survivingStop = AddPendingStop(route, [Guid.NewGuid()], 1);
        order.AssignToRoute(route.Id);

        SetupRepositories(order, route);

        var result = await _handler.Handle(new CancelOrderCommand(order.Id), default);

        result.IsSuccess.Should().BeTrue();
        route.Stops.Should().ContainSingle().Which.Should().Be(survivingStop);
        survivingStop.Sequence.Should().Be(0);
    }

    [Fact]
    public async Task Handle_StopAlreadyInProgress_ReturnsFailure()
    {
        var order = CreateIndividualOrder();
        var route = Route.Create(Guid.NewGuid()).Value!;
        var stop = AddPendingStop(route, [order.Id], 0);
        stop.Start();
        order.AssignToRoute(route.Id);

        SetupRepositories(order, route);

        var result = await _handler.Handle(new CancelOrderCommand(order.Id), default);

        result.IsFailure.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.AssignedToRoute);
        stop.Orders.Should().Contain(order.Id);
    }

    [Fact]
    public async Task Handle_AssignedOrderButRouteMissing_StillCancels()
    {
        var order = CreateIndividualOrder();
        order.AssignToRoute(Guid.NewGuid());

        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var result = await _handler.Handle(new CancelOrderCommand(order.Id), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    #endregion

    #region Helpers

    private void SetupRepositories(Order order, Route route)
    {
        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
    }

    private static Stop AddPendingStop(Route route, IReadOnlyList<Guid> orders, int sequence)
    {
        var address = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var stop = Stop.Create(route.Id, address, location, null, sequence, orders).Value!;
        route.AddStop(stop);
        return stop;
    }

    private static IndividualOrder CreateIndividualOrder()
    {
        var address = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var weight = Weight.Create(5m).Value!;
        var volume = Volume.Create(0.5m).Value!;
        var phone = PhoneNumber.Create("+48601234567").Value!;
        var window = DeliveryWindow.AnyTime();

        return IndividualOrder.Create(
            Guid.NewGuid(), address, location, weight, volume, window,
            phone, CargoType.General, null, "Jan Kowalski", false).Value!;
    }

    #endregion
}
