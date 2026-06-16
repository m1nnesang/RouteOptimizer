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

public class StopFailedNotificationHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();
    private readonly StopFailedNotificationHandler _handler;

    public StopFailedNotificationHandlerTests()
    {
        _handler = new StopFailedNotificationHandler(
            _routeRepository.Object,
            _orderRepository.Object,
            _unitOfWork.Object,
            _notifier.Object,
            NullLogger<StopFailedNotificationHandler>.Instance);
    }

    #region Tests

    [Fact]
    public async Task Handle_FailsStopOrders_AdvancesAndMarksNextInTransit()
    {
        var (route, failedStop, nextStop, failedOrder, nextOrder) = CreateRouteScenario();

        await _handler.Handle(new StopFailed(failedStop.Id, route.Id), default);

        failedOrder.Status.Should().Be(OrderStatus.Failed);
        nextOrder.Status.Should().Be(OrderStatus.InTransit);
        nextStop.Status.Should().Be(StopStatus.InProgress);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_NotifiesDispatchersWithNextStop()
    {
        var (route, failedStop, nextStop, _, _) = CreateRouteScenario();

        await _handler.Handle(new StopFailed(failedStop.Id, route.Id), default);

        _notifier.Verify(x => x.StopFailedAsync(
            route.WarehouseId, route.Id, failedStop.Id, nextStop.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NotifierThrows_DoesNotPropagate()
    {
        var (route, failedStop, _, _, _) = CreateRouteScenario();
        _notifier
            .Setup(x => x.StopFailedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        var act = () => _handler.Handle(new StopFailed(failedStop.Id, route.Id), default);

        await act.Should().NotThrowAsync();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RouteNotFound_DoesNothing()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        await _handler.Handle(new StopFailed(Guid.NewGuid(), Guid.NewGuid()), default);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifier.VerifyNoOtherCalls();
    }

    #endregion

    #region Helpers

    private (Route Route, Stop FailedStop, Stop NextStop, Order FailedOrder, Order NextOrder)
        CreateRouteScenario()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;

        var failedOrder = CreateIndividualOrder();
        var nextOrder = CreateIndividualOrder();

        var failedStop = CreateStop(route.Id, [failedOrder.Id], 0);
        var nextStop = CreateStop(route.Id, [nextOrder.Id], 1);
        route.AddStop(failedStop);
        route.AddStop(nextStop);

        failedStop.Start();

        failedOrder.AssignToRoute(route.Id);
        failedOrder.MarkAsInTransit();
        nextOrder.AssignToRoute(route.Id);

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(failedStop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failedOrder]);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(nextStop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync([nextOrder]);

        return (route, failedStop, nextStop, failedOrder, nextOrder);
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
