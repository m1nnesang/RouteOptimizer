using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Application.Routes.HandoverRoute;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class HandoverRouteCommandHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IDriverShiftRepository> _shiftRepo = new();
    private readonly Mock<IRouteOptimizer> _optimizer = new();
    private readonly Mock<IDistanceMatrixProvider> _matrixProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly HandoverRouteCommandHandler _handler;

    public HandoverRouteCommandHandlerTests()
    {
        _optimizer.Setup(x => x.OptimizeAsync(It.IsAny<RouteOptimizerInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteOptimizerOutput { OrderedStopIds = [], TotalDurationSeconds = 100, AlgorithmName = "Test" });
        _matrixProvider.Setup(x => x.GetMatrixAsync(It.IsAny<(double, double)>(), It.IsAny<IReadOnlyList<(double, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistanceMatrix(new double[0, 0]));

        _handler = new HandoverRouteCommandHandler(
            _routeRepo.Object,
            _orderRepo.Object,
            _warehouseRepo.Object,
            _shiftRepo.Object,
            [_optimizer.Object],
            _matrixProvider.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ReturnsFailure()
    {
        _routeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var result = await _handler.Handle(
            new HandoverRouteCommand(Guid.NewGuid(), HandoverType.ReturnToPool, null, null), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RouteInDraftStatus_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepo.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(
            new HandoverRouteCommand(route.Id, HandoverType.ReturnToPool, null, null), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnToPool_InterruptsRouteAndReturnsOrdersToPool()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        var order = CreateOrder(warehouseId);
        order.AssignToRoute(route.Id);

        var stop = Stop.Create(route.Id, order.Address, order.Location, null, 0, [order.Id]).Value!;
        route.AddStop(stop);

        SetupRoute(route);
        SetupWarehouse(warehouseId);
        _orderRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);

        var result = await _handler.Handle(
            new HandoverRouteCommand(route.Id, HandoverType.ReturnToPool, null, null), default);

        result.IsSuccess.Should().BeTrue();
        route.Status.Should().Be(RouteStatus.Interrupted);
        order.Status.Should().Be(OrderStatus.Created);
        order.AssignedRouteId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TransferAll_ShiftNotFound_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        SetupRoute(route);
        SetupWarehouse(warehouseId);
        _orderRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _shiftRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);

        var result = await _handler.Handle(
            new HandoverRouteCommand(route.Id, HandoverType.TransferAll, Guid.NewGuid(), null), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TransferAll_ShiftDifferentWarehouse_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        SetupRoute(route);
        SetupWarehouse(warehouseId);
        _orderRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var shift = CreateShift(Guid.NewGuid()); // разный warehouse
        _shiftRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(
            new HandoverRouteCommand(route.Id, HandoverType.TransferAll, shift.Id, null), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Distribute_StopIdNotInRoute_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateInProgressRoute(warehouseId);
        SetupRoute(route);
        SetupWarehouse(warehouseId);

        var assignments = new List<ShiftStopsAssignment>
        {
            new(Guid.NewGuid(), [Guid.NewGuid()]) // стоп не в маршруте
        };

        var result = await _handler.Handle(
            new HandoverRouteCommand(route.Id, HandoverType.Distribute, null, assignments), default);

        result.IsFailure.Should().BeTrue();
    }

    private void SetupRoute(Route route) =>
        _routeRepo.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

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

    private static DriverShift CreateShift(Guid warehouseId) =>
        DriverShift.Create(Guid.NewGuid(), Guid.NewGuid(), warehouseId,
            DateOnly.FromDateTime(DateTime.UtcNow)).Value!;
}
