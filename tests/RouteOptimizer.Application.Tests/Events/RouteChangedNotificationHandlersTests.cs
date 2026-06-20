using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Events;
using RouteOptimizer.Domain.Events.Route;

namespace RouteOptimizer.Application.Tests.Events;

public class RouteChangedNotificationHandlersTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IRouteEventsNotifier> _notifier = new();

    private UrgentOrderInsertedNotificationHandler CreateHandler() =>
        new(_routeRepository.Object, _notifier.Object,
            NullLogger<UrgentOrderInsertedNotificationHandler>.Instance);

    [Fact]
    public async Task Handle_RouteExists_BroadcastsToWarehouseGroup()
    {
        var warehouseId = Guid.NewGuid();
        var route = Domain.Entities.Route.Route.Create(warehouseId).Value!;
        _routeRepository.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        await CreateHandler().Handle(new UrgentOrderInserted(route.Id), default);

        _notifier.Verify(x => x.RouteChangedAsync(
            warehouseId, route.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RouteNotFound_DoesNotBroadcast()
    {
        _routeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Route.Route?)null);

        await CreateHandler().Handle(new UrgentOrderInserted(Guid.NewGuid()), default);

        _notifier.Verify(x => x.RouteChangedAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotifierThrows_DoesNotPropagate()
    {
        var route = Domain.Entities.Route.Route.Create(Guid.NewGuid()).Value!;
        _routeRepository.Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _notifier.Setup(x => x.RouteChangedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = async () => await CreateHandler().Handle(new UrgentOrderInserted(route.Id), default);

        await act.Should().NotThrowAsync();
    }
}
