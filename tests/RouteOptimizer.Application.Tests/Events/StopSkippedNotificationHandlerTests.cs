using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Events;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Stop;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Events;

public class StopSkippedNotificationHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();
    private readonly StopSkippedNotificationHandler _handler;

    public StopSkippedNotificationHandlerTests()
    {
        _handler = new StopSkippedNotificationHandler(
            _routeRepository.Object,
            _orderRepository.Object,
            _unitOfWork.Object,
            _notifier.Object,
            NullLogger<StopSkippedNotificationHandler>.Instance);
    }

    #region Tests

    [Fact]
    public async Task Handle_AdvancesToNextStop_MarksInTransitAndSaves()
    {
        var (route, skippedStop, nextStop, nextOrder) = CreateRouteScenario();

        await _handler.Handle(new StopSkipped(skippedStop.Id, route.Id), default);

        nextOrder.Status.Should().Be(OrderStatus.InTransit);
        nextStop.Status.Should().Be(StopStatus.InProgress);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_NotifiesDispatchersWithNextStop()
    {
        var (route, skippedStop, nextStop, _) = CreateRouteScenario();

        await _handler.Handle(new StopSkipped(skippedStop.Id, route.Id), default);

        _notifier.Verify(x => x.StopSkippedAsync(
            route.WarehouseId, route.Id, skippedStop.Id, nextStop.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoNextStop_DoesNotSaveButNotifies()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        var onlyStop = CreateStop(route.Id, [Guid.NewGuid()], 0);
        route.AddStop(onlyStop);
        onlyStop.Start();
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        await _handler.Handle(new StopSkipped(onlyStop.Id, route.Id), default);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifier.Verify(x => x.StopSkippedAsync(
            route.WarehouseId, route.Id, onlyStop.Id, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NotifierThrows_DoesNotPropagate()
    {
        var (route, skippedStop, _, _) = CreateRouteScenario();
        _notifier
            .Setup(x => x.StopSkippedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        var act = () => _handler.Handle(new StopSkipped(skippedStop.Id, route.Id), default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_RouteNotFound_DoesNothing()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        await _handler.Handle(new StopSkipped(Guid.NewGuid(), Guid.NewGuid()), default);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifier.VerifyNoOtherCalls();
    }

    #endregion

    #region Helpers

    private (Route Route, Stop SkippedStop, Stop NextStop, Order NextOrder) CreateRouteScenario()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;

        var nextOrder = CreateIndividualOrder();

        var skippedStop = CreateStop(route.Id, [Guid.NewGuid()], 0);
        var nextStop = CreateStop(route.Id, [nextOrder.Id], 1);
        route.AddStop(skippedStop);
        route.AddStop(nextStop);

        skippedStop.Start();
        nextOrder.AssignToRoute(route.Id);

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(nextStop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync([nextOrder]);

        return (route, skippedStop, nextStop, nextOrder);
    }

    private static Stop CreateStop(Guid routeId, IReadOnlyList<Guid> orders, int sequence)
    {
        var address = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        return Stop.Create(routeId, address, location, null, sequence, orders).Value!;
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

    #endregion
}
