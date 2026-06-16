using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Events;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Route;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Events;

public class RouteStartedNotificationHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();
    private readonly RouteStartedNotificationHandler _handler;

    public RouteStartedNotificationHandlerTests()
    {
        _handler = new RouteStartedNotificationHandler(
            _routeRepository.Object,
            _orderRepository.Object,
            _unitOfWork.Object,
            _notifier.Object,
            NullLogger<RouteStartedNotificationHandler>.Instance);
    }

    #region Tests

    [Fact]
    public async Task Handle_StartsFirstStop_MarksOrdersInTransitAndSaves()
    {
        var (route, firstStop, firstOrder) = CreateRouteScenario();

        await _handler.Handle(new RouteStarted(route.Id, Guid.NewGuid()), default);

        firstStop.Status.Should().Be(StopStatus.InProgress);
        firstOrder.Status.Should().Be(OrderStatus.InTransit);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_NotifiesDispatchers()
    {
        var (route, _, _) = CreateRouteScenario();

        await _handler.Handle(new RouteStarted(route.Id, Guid.NewGuid()), default);

        _notifier.Verify(x => x.RouteStartedAsync(
            route.WarehouseId, route.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotifierThrows_DoesNotPropagate()
    {
        var (route, _, _) = CreateRouteScenario();
        _notifier
            .Setup(x => x.RouteStartedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        var act = () => _handler.Handle(new RouteStarted(route.Id, Guid.NewGuid()), default);

        await act.Should().NotThrowAsync();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RouteNotFound_DoesNothing()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        await _handler.Handle(new RouteStarted(Guid.NewGuid(), Guid.NewGuid()), default);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifier.VerifyNoOtherCalls();
    }

    #endregion

    #region Helpers

    private (Route Route, Stop FirstStop, Order FirstOrder) CreateRouteScenario()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;

        var firstOrder = CreateIndividualOrder();
        var secondOrder = CreateIndividualOrder();
        firstOrder.AssignToRoute(route.Id);
        secondOrder.AssignToRoute(route.Id);

        var firstStop = CreateStop(route.Id, [firstOrder.Id], 0);
        var secondStop = CreateStop(route.Id, [secondOrder.Id], 1);
        route.AddStop(firstStop);
        route.AddStop(secondStop);

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _orderRepository
            .Setup(x => x.GetByIdsAsync(firstStop.Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstOrder]);

        return (route, firstStop, firstOrder);
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
