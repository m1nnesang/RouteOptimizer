using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Orders.GetDeliveryAttempts;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Orders;

public class GetOrderDeliveryAttemptsQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IDeliveryAttemptRepository> _attemptRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetOrderDeliveryAttemptsQueryHandler _handler;

    public GetOrderDeliveryAttemptsQueryHandlerTests()
    {
        _handler = new GetOrderDeliveryAttemptsQueryHandler(
            _orderRepository.Object, _attemptRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        _orderRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(new GetOrderDeliveryAttemptsQuery(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_OrderInOtherWarehouse_ReturnsFailure()
    {
        var order = CreateOrder();
        _orderRepository
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUser.Setup(u => u.WarehouseId).Returns(Guid.NewGuid());

        var result = await _handler.Handle(new GetOrderDeliveryAttemptsQuery(order.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_OrderFound_ReturnsMappedAttempts()
    {
        var order = CreateOrder();
        var location = GeoCoordinate.Create(52.1, 21.0).Value!;
        var failed = DeliveryAttempt.Create(order.Id, Guid.NewGuid(), Guid.NewGuid(), location,
            DeliveryOutcome.Failed, FailureReason.CustomerNotAtHome, "Nobody home", "photo-key-1").Value!;
        var delivered = DeliveryAttempt.Create(order.Id, Guid.NewGuid(), Guid.NewGuid(), location,
            DeliveryOutcome.Delivered, null, null, null).Value!;

        _orderRepository
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _attemptRepository
            .Setup(x => x.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failed, delivered]);

        var result = await _handler.Handle(new GetOrderDeliveryAttemptsQuery(order.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var failedDto = result.Value!.Single(a => a.Outcome == "Failed");
        failedDto.OrderId.Should().Be(order.Id);
        failedDto.FailureReason.Should().Be("CustomerNotAtHome");
        failedDto.Notes.Should().Be("Nobody home");
        failedDto.PhotoKey.Should().Be("photo-key-1");
        failedDto.Latitude.Should().Be(52.1);

        var deliveredDto = result.Value!.Single(a => a.Outcome == "Delivered");
        deliveredDto.FailureReason.Should().BeNull();
        deliveredDto.PhotoKey.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoAttempts_ReturnsEmptyList()
    {
        var order = CreateOrder();
        _orderRepository
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _attemptRepository
            .Setup(x => x.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetOrderDeliveryAttemptsQuery(order.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static IndividualOrder CreateOrder()
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
}
