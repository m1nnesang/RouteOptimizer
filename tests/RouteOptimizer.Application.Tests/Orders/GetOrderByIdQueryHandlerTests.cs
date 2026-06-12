using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Orders.GetOrderById;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Orders;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _handler = new GetOrderByIdQueryHandler(_orderRepository.Object);
    }

    #region Failure Cases

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

    #endregion

    #region Happy Path

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
        result.Value.CustomerName.Should().Be("Jan Kowalski");
        result.Value.City.Should().Be("Warszawa");
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
        result.Value.CompanyName.Should().Be("Firma Logistyczna Sp. z o.o.");
        result.Value.CustomerName.Should().BeNull();
    }

    #endregion

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
