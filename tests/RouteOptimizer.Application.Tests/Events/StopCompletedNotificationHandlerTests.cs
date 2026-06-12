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

public class StopCompletedNotificationHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();
    private readonly StopCompletedNotificationHandler _handler;

    public StopCompletedNotificationHandlerTests()
    {
        _handler = new StopCompletedNotificationHandler(
            _routeRepository.Object,
            _orderRepository.Object,
            _unitOfWork.Object,
            _notifier.Object,
            NullLogger<StopCompletedNotificationHandler>.Instance);
    }

    #region Tests

    [Fact]
    public async Task Handle_FullCompletion_MarksOrdersDeliveredAndAdvancesRoute()
    {
        var (route, completedStop, nextStop, completedOrder, nextOrder) = CreateRouteScenario();

        var notification = new StopCompleted(completedStop.Id, route.Id, IsPartial: false);
        await _handler.Handle(notification, default);

        completedOrder.Status.Should().Be(OrderStatus.Delivered);
        nextOrder.Status.Should().Be(OrderStatus.InTransit);
        nextStop.Status.Should().Be(StopStatus.InProgress);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialCompletion_DoesNotMarkOrdersDelivered()
    {
        var (route, completedStop, _, completedOrder, _) = CreateRouteScenario();

        var notification = new StopCompleted(completedStop.Id, route.Id, IsPartial: true);
        await _handler.Handle(notification, default);

        completedOrder.Status.Should().NotBe(OrderStatus.Delivered);
    }

    [Fact]
    public async Task Handle_Success_NotifiesDispatchersAfterSave()
    {
        var (route, completedStop, nextStop, _, _) = CreateRouteScenario();

        var notification = new StopCompleted(completedStop.Id, route.Id, IsPartial: false);
        await _handler.Handle(notification, default);

        _notifier.Verify(x => x.StopCompletedAsync(
            route.WarehouseId, route.Id, completedStop.Id, nextStop.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NotifierThrows_DoesNotPropagate()
    {
        var (route, completedStop, _, _, _) = CreateRouteScenario();

        _notifier
            .Setup(x => x.StopCompletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        var notification = new StopCompleted(completedStop.Id, route.Id, IsPartial: false);

        var act = () => _handler.Handle(notification, default);

        await act.Should().NotThrowAsync();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RouteNotFound_DoesNothing()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        await _handler.Handle(new StopCompleted(Guid.NewGuid(), Guid.NewGuid(), false), default);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifier.VerifyNoOtherCalls();
    }

    #endregion

    #region Helpers

    private (Route Route, Stop CompletedStop, Stop NextStop, Order CompletedOrder, Order NextOrder)
        CreateRouteScenario()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;

        var completedOrder = CreateIndividualOrder();
        var nextOrder = CreateIndividualOrder();

        var completedStop = CreateStop(route.Id, [completedOrder.Id], 0);
        var nextStop = CreateStop(route.Id, [nextOrder.Id], 1);
        route.AddStop(completedStop);
        route.AddStop(nextStop);

        completedStop.Start();
        completedStop.Complete();

        completedOrder.AssignToRoute(route.Id);
        completedOrder.MarkAsInTransit();
        nextOrder.AssignToRoute(route.Id);

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(completedStop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync([completedOrder]);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(nextStop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync([nextOrder]);

        return (route, completedStop, nextStop, completedOrder, nextOrder);
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
