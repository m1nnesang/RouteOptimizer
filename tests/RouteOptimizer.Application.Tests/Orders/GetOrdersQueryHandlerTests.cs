using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Application.Orders.GetOrders;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Orders;

public class GetOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetOrdersQueryHandler _handler;

    public GetOrdersQueryHandlerTests()
    {
        _handler = new GetOrdersQueryHandler(_orderRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _orderRepository
            .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<OrderStatus?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Order>(), 0));

        var query = new GetOrdersQuery(null, null);
        var result = await _handler.Handle(query, default);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BusinessOrder_MapsCorrectly()
    {
        var order = CreateBusinessOrder();
        _orderRepository
            .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<OrderStatus?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Order> { order }, 1));

        var query = new GetOrdersQuery(null, null);
        var result = await _handler.Handle(query, default);

        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];
        dto.Id.Should().Be(order.Id);
        dto.OrderType.Should().Be("Business");
        dto.CompanyName.Should().Be("Firma Logistyczna Sp. z o.o.");
        dto.CustomerName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_IndividualOrder_MapsCorrectly()
    {
        var order = CreateIndividualOrder();
        _orderRepository
            .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<OrderStatus?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Order> { order }, 1));

        var query = new GetOrdersQuery(null, null);
        var result = await _handler.Handle(query, default);

        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];
        dto.Id.Should().Be(order.Id);
        dto.OrderType.Should().Be("Individual");
        dto.CustomerName.Should().Be("Jan Kowalski");
        dto.CompanyName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MixedOrders_ReturnsBothMapped()
    {
        var business = CreateBusinessOrder();
        var individual = CreateIndividualOrder();
        _orderRepository
            .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<OrderStatus?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Order> { business, individual }, 2));

        var query = new GetOrdersQuery(null, null);
        var result = await _handler.Handle(query, default);

        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainSingle(x => x.OrderType == "Business");
        result.Items.Should().ContainSingle(x => x.OrderType == "Individual");
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

    private static BusinessOrder CreateBusinessOrder()
    {
        var address = Address.Create("ul. Floriańska 3", "Kraków", "31-019", "Poland").Value!;
        var location = GeoCoordinate.Create(50.0614, 19.9366).Value!;
        var weight = Weight.Create(10m).Value!;
        var volume = Volume.Create(1m).Value!;
        var phone = PhoneNumber.Create("+48122345678").Value!;
        var window = DeliveryWindow.AnyTime();

        return BusinessOrder.Create(
            Guid.NewGuid(), address, location, weight, volume, window,
            phone, CargoType.General, null, "Firma Logistyczna Sp. z o.o.", "Anna Nowak").Value!;
    }
}
