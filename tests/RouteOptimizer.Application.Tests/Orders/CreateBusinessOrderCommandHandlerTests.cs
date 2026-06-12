using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Orders.CreateBusinessOrder;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Orders;

public class CreateBusinessOrderCommandHandlerTests
{
    private readonly Mock<IGeocodingService> _geocodingService = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly CreateBusinessOrderCommandHandler _handler;

    public CreateBusinessOrderCommandHandlerTests()
    {
        _handler = new CreateBusinessOrderCommandHandler(_orderRepository.Object, _geocodingService.Object);
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
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
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
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
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
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
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
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { CargoType = "NotACargo" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid cargo type");
    }

    [Fact]
    public async Task Handle_InvalidWindowStrictness_ReturnsFailure()
    {
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { WindowStrictness = "NotAStrictness" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid window strictness");
    }

    [Fact]
    public async Task Handle_EmptyCompanyName_ReturnsFailure()
    {
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
        _geocodingService
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(geo));

        var command = CreateValidCommand() with { CompanyName = "" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsId()
    {
        var geo = GeoCoordinate.Create(50.06, 19.94).Value!;
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

    private static CreateBusinessOrderCommand CreateValidCommand() =>
        new(
            WarehouseId: Guid.NewGuid(),
            CompanyName: "Firma Logistyczna Sp. z o.o.",
            ContactPerson: "Anna Nowak",
            Start: new TimeOnly(9, 0),
            End: new TimeOnly(17, 0),
            Street: "ul. Floriańska 3",
            City: "Kraków",
            Postcode: "31-019",
            Country: "Poland",
            PhoneNumber: "+48122345678",
            Weight: 10m,
            Volume: 1m,
            CargoType: "General",
            Notes: null,
            WindowStrictness: "Soft"
        );
}
