using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.CreateRoute;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class CreateRouteCommandHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly CreateRouteCommandHandler _handler;

    public CreateRouteCommandHandlerTests()
    {
        _handler = new CreateRouteCommandHandler(
            _warehouseRepository.Object,
            _orderRepository.Object,
            _routeRepository.Object,
            _currentUser.Object);
    }

    [Fact]
    public async Task Handle_WarehouseNotFound_ThrowsNotFoundException()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        _warehouseRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var act = () => _handler.Handle(new CreateRouteCommand([Guid.NewGuid()], default), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrdersNotFound_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        SetupWarehouse(warehouseId);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new CreateRouteCommand([Guid.NewGuid()], default), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrderNotInCreatedStatus_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        SetupWarehouse(warehouseId);

        var order = CreateOrder(warehouseId);
        order.AssignToRoute(Guid.NewGuid());

        _orderRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);

        var result = await _handler.Handle(new CreateRouteCommand([order.Id], default), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrderFromDifferentWarehouse_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        SetupWarehouse(warehouseId);

        var order = CreateOrder(Guid.NewGuid());

        _orderRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);

        var result = await _handler.Handle(new CreateRouteCommand([order.Id], default), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesRouteAndReturnsId()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        SetupWarehouse(warehouseId);

        var order = CreateOrder(warehouseId);

        _orderRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        _routeRepository
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Route.Route>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CreateRouteCommand([order.Id], default), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_OrdersAtSameAddress_AreGroupedIntoSingleStop()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        SetupWarehouse(warehouseId);

        var first = CreateOrder(warehouseId, "ul. Wspolna 5", apartment: "12");
        var second = CreateOrder(warehouseId, "ul. Wspolna 5", apartment: "34");
        var other = CreateOrder(warehouseId, "ul. Inna 7");

        _orderRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second, other]);

        Domain.Entities.Route.Route? captured = null;
        _routeRepository
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Route.Route>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Route.Route, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CreateRouteCommand([first.Id, second.Id, other.Id], default), default);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Stops.Should().HaveCount(2);

        var groupedStop = captured.Stops.Single(s => s.Orders.Count == 2);
        groupedStop.Orders.Should().BeEquivalentTo([first.Id, second.Id]);
        captured.Stops.Select(s => s.Sequence).Should().BeEquivalentTo([0, 1]);
    }

    private void SetupWarehouse(Guid warehouseId)
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        var warehouse = Warehouse.Create("Magazyn Testowy", address, location).Value!;
        _warehouseRepository
            .Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
    }

    private static BusinessOrder CreateOrder(Guid warehouseId, string street = "ul. Testowa 1", string? apartment = null)
    {
        var address = Address.Create(street, "Warszawa", "00-001", "Poland", apartment).Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        var weight = Weight.Create(10m).Value!;
        var volume = Volume.Create(1m).Value!;
        var window = DeliveryWindow.AnyTime();
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return BusinessOrder.Create(warehouseId, address, location, weight, volume, window, phone,
            CargoType.General, null, "Firma Testowa", "Jan Kowalski").Value!;
    }
}
