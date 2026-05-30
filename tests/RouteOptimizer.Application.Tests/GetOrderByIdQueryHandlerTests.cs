using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Orders.GetOrderById;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _handler = new GetOrderByIdQueryHandler(_orderRepository.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var query = new GetOrderByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_IndividualOrderFound_ReturnsMappedDto()
    {
        var order = CreateIndividualOrder();

        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)order);

        var query = new GetOrderByIdQuery(order.Id);

        var result = await _handler.Handle(query, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(order.Id);
        result.Value.CustomerName.Should().Be("Ivan Ivanov");
        result.Value.City.Should().Be("Moscow");
        result.Value.CompanyName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BusinessOrderFound_ReturnsMappedDto()
    {
        var order = CreateBusinessOrder();

        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)order);

        var query = new GetOrderByIdQuery(order.Id);

        var result = await _handler.Handle(query, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(order.Id);
        result.Value.CompanyName.Should().Be("Acme Corp");
        result.Value.CustomerName.Should().BeNull();
    }

    private static IndividualOrder CreateIndividualOrder()
    {
        var address = Address.Create("Tverskaya 1", "Moscow", "125009", "Russia").Value!;
        var location = GeoCoordinate.Create(55.75, 37.61).Value!;
        var weight = Weight.Create(5m).Value!;
        var volume = Volume.Create(0.5m).Value!;
        var phone = PhoneNumber.Create("+79001234567").Value!;
        var window = DeliveryWindow.AnyTime();

        return IndividualOrder.Create(
            Guid.NewGuid(), address, location, weight, volume, window,
            phone, CargoType.General, null, "Ivan Ivanov", false).Value!;
    }

    private static BusinessOrder CreateBusinessOrder()
    {
        var address = Address.Create("Lenina 5", "Saint Petersburg", "190000", "Russia").Value!;
        var location = GeoCoordinate.Create(59.93, 30.32).Value!;
        var weight = Weight.Create(10m).Value!;
        var volume = Volume.Create(1m).Value!;
        var phone = PhoneNumber.Create("+79009876543").Value!;
        var window = DeliveryWindow.AnyTime();

        return BusinessOrder.Create(
            Guid.NewGuid(), address, location, weight, volume, window,
            phone, CargoType.General, null, "Acme Corp", "Jane Doe").Value!;
    }
}
