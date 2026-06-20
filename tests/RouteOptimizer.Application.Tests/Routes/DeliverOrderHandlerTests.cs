using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Orders.DeliverOrder;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class DeliverOrderHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly DeliverOrderHandler _handler;

    public DeliverOrderHandlerTests()
    {
        _handler = new DeliverOrderHandler(_routeRepository.Object, _orderRepository.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new DeliverOrderCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RouteNotInProgress_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new DeliverOrderCommand(route.Id, Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StopNotInProgress_ReturnsFailure()
    {
        var route = CreateInProgressRoute();
        var stop = CreatePendingStop(route, [Guid.NewGuid()]);
        SetupRoute(route);

        var result = await _handler.Handle(new DeliverOrderCommand(route.Id, stop.Id, stop.Orders[0]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not in progress");
    }

    [Fact]
    public async Task Handle_OrderNotInStop_ThrowsNotFoundException()
    {
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route, [Guid.NewGuid()]);
        SetupRoute(route);

        var act = () => _handler.Handle(new DeliverOrderCommand(route.Id, stop.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OneOfTwoDelivered_StopStaysInProgress()
    {
        var first = CreateInTransitOrder();
        var second = CreateInTransitOrder();
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route, [first.Id, second.Id]);
        SetupRoute(route);
        SetupOrders(stop, first, second);

        var result = await _handler.Handle(new DeliverOrderCommand(route.Id, stop.Id, first.Id), default);

        result.IsSuccess.Should().BeTrue();
        first.Status.Should().Be(OrderStatus.Delivered);
        stop.Status.Should().Be(StopStatus.InProgress);
    }

    [Fact]
    public async Task Handle_LastOrderDelivered_StopBecomesCompleted()
    {
        var order = CreateInTransitOrder();
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route, [order.Id]);
        SetupRoute(route);
        SetupOrders(stop, order);

        var result = await _handler.Handle(new DeliverOrderCommand(route.Id, stop.Id, order.Id), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.Completed);
    }

    [Fact]
    public async Task Handle_LastOrderDeliveredWithEarlierFailure_StopBecomesPartiallyCompleted()
    {
        var delivered = CreateInTransitOrder();
        var failed = CreateInTransitOrder();
        failed.MarkAsFailed();
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route, [delivered.Id, failed.Id]);
        SetupRoute(route);
        SetupOrders(stop, delivered, failed);

        var result = await _handler.Handle(new DeliverOrderCommand(route.Id, stop.Id, delivered.Id), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.PartiallyCompleted);
    }

    private void SetupRoute(Route route) =>
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

    private void SetupOrders(Stop stop, params Order[] orders) =>
        _orderRepository
            .Setup(x => x.GetByIdsAsync(stop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

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

    private static Route CreateInProgressRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        return route;
    }

    private static Stop CreatePendingStop(Route route, IReadOnlyList<Guid> orders)
    {
        var address = Address.Create("ul. Marszalkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, orders).Value!;
        route.AddStop(stop);
        return stop;
    }

    private static Stop CreateInProgressStop(Route route, IReadOnlyList<Guid> orders)
    {
        var stop = CreatePendingStop(route, orders);
        stop.Start();
        return stop;
    }
}
