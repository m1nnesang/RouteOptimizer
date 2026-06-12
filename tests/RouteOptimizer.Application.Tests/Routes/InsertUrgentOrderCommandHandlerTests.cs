using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Application.Routes.InsertUrgentOrder;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class InsertUrgentOrderCommandHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IRouteOptimizer> _optimizer = new();
    private readonly Mock<IDistanceMatrixProvider> _matrixProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly InsertUrgentOrderCommandHandler _handler;

    public InsertUrgentOrderCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _matrixProvider.Setup(x => x.GetMatrixAsync(It.IsAny<(double, double)>(), It.IsAny<IReadOnlyList<(double, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistanceMatrix(new double[0, 0]));

        _handler = new InsertUrgentOrderCommandHandler(
            _routeRepo.Object,
            _orderRepo.Object,
            _warehouseRepo.Object,
            [_optimizer.Object],
            _matrixProvider.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ReturnsFailure()
    {
        _routeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var result = await _handler.Handle(new InsertUrgentOrderCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        var route = CreateInProgressRoute(Guid.NewGuid());
        _routeRepo.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessOrder?)null);

        var result = await _handler.Handle(new InsertUrgentOrderCommand(route.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrderDifferentWarehouse_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        var order = CreateOrder(Guid.NewGuid());

        _routeRepo.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepo.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(new InsertUrgentOrderCommand(route.Id, order.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoOptimizersConfigured_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        var order = CreateOrder(warehouseId);

        _routeRepo.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepo.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        SetupWarehouse(warehouseId);

        var handlerNoOptimizers = new InsertUrgentOrderCommandHandler(
            _routeRepo.Object, _orderRepo.Object, _warehouseRepo.Object,
            [], _matrixProvider.Object, _unitOfWork.Object);

        var result = await handlerNoOptimizers.Handle(new InsertUrgentOrderCommand(route.Id, order.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsStopToRoute()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        var order = CreateOrder(warehouseId);

        _routeRepo.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepo.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        SetupWarehouse(warehouseId);
        _optimizer.Setup(x => x.OptimizeAsync(It.IsAny<RouteOptimizerInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RouteOptimizerInput input, CancellationToken _) =>
                new RouteOptimizerOutput { OrderedStopIds = input.Stops.Select(s => s.StopId).ToList(), TotalDurationSeconds = 100, AlgorithmName = "Test" });

        var result = await _handler.Handle(new InsertUrgentOrderCommand(route.Id, order.Id), default);

        result.IsSuccess.Should().BeTrue();
        route.Stops.Should().HaveCount(1);
        order.AssignedRouteId.Should().Be(route.Id);
    }

    private void SetupWarehouse(Guid warehouseId)
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        var warehouse = Warehouse.Create("Magazyn", address, location).Value!;
        _warehouseRepo.Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
    }

    private static Route CreateInProgressRoute(Guid warehouseId)
    {
        var route = Route.Create(warehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        return route;
    }

    private static BusinessOrder CreateOrder(Guid warehouseId)
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        var weight = Weight.Create(10m).Value!;
        var volume = Volume.Create(1m).Value!;
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return BusinessOrder.Create(warehouseId, address, location, weight, volume,
            DeliveryWindow.AnyTime(), phone, CargoType.General, null, "Firma", "Jan").Value!;
    }
}
