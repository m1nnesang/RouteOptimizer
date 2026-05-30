using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Orders.CreateIndividualOrder;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests;

public class CreateIndividualOrderCommandHandlerTests
{
    private readonly Mock<IGeocodingService> _geocodingService = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly CreateIndividualOrderCommandHandler _handler;

    public CreateIndividualOrderCommandHandlerTests()
    {
        _handler = new CreateIndividualOrderCommandHandler(
            _geocodingService.Object,
            _orderRepository.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_InvalidAddress_ReturnsFailure()
    {
        var command = CreateValidCommand() with { Street = "" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GeocodingFails_ReturnsFailure()
    {
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Failure("Geocoding failed"));

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Geocoding failed");
    }

    [Fact]
    public async Task Handle_InvalidWeight_ReturnsFailure()
    {
        var geo = GeoCoordinate.Create(55.75, 37.61).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { Weight = -1m };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidVolume_ReturnsFailure()
    {
        var geo = GeoCoordinate.Create(55.75, 37.61).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { Volume = -1m };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidPhoneNumber_ReturnsFailure()
    {
        var geo = GeoCoordinate.Create(55.75, 37.61).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { PhoneNumber = "" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidCargoType_ReturnsFailure()
    {
        var geo = GeoCoordinate.Create(55.75, 37.61).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { CargoType = "NotACargo" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid cargo type");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsId()
    {
        var geo = GeoCoordinate.Create(55.75, 37.61).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));
        _orderRepository
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _orderRepository.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    private static CreateIndividualOrderCommand CreateValidCommand() =>
        new(
            WarehouseId: Guid.NewGuid(),
            Street: "ul. Marszałkowska 1",
            City: "Warszawa",
            Postcode: "00-001",
            Country: "Poland",
            CustomerName: "Jan Kowalski",
            PhoneNumber: "+48601234567",
            Weight: 5m,
            Volume: 0.5m,
            CargoType: "General",
            AllowLeaveAtDoor: false,
            Notes: null,
            Start: null,
            End: null,
            WindowStrictness: null
        );
}
