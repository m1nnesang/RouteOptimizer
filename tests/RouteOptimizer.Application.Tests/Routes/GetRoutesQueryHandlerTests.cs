using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Application.Routes.GetRoutes;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class GetRoutesQueryHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetRoutesQueryHandler _handler;

    public GetRoutesQueryHandlerTests()
    {
        _shiftRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriverShift>());
        _userRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _warehouseRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse>());

        _handler = new GetRoutesQueryHandler(
            _routeRepository.Object,
            _shiftRepository.Object,
            _userRepository.Object,
            _warehouseRepository.Object,
            _currentUser.Object);
    }

    [Fact]
    public async Task Handle_NoRoutes_ReturnsEmptyList()
    {
        _routeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<RouteStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Route>(), 0));

        var result = await _handler.Handle(new GetRoutesQuery(null), default);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RoutesExist_ReturnsMappedDtos()
    {
        var route1 = CreateRouteWithStop();
        var route2 = CreateRouteWithStop();

        _routeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<RouteStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Route> { route1, route2 }, 2));

        var result = await _handler.Handle(new GetRoutesQuery(null), default);

        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(route1.Id);
        result.Items[0].StopsCount.Should().Be(1);
        result.Items[0].Status.Should().Be(route1.Status.ToString());
        result.Items[0].AssignedShiftId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FiltersPassedToRepository()
    {
        var warehouseId = Guid.NewGuid();
        var status = RouteStatus.InProgress;

        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);

        _routeRepository
            .Setup(x => x.GetAllAsync(warehouseId, status, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Route>(), 0));

        await _handler.Handle(new GetRoutesQuery(status), default);

        _routeRepository.Verify(
            x => x.GetAllAsync(warehouseId, status, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Route CreateRouteWithStop()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        var address = Address.Create("ul. Testowa 1", "Krakow", "30-001", "Poland").Value!;
        var location = GeoCoordinate.Create(50.0647, 19.9450).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, []).Value!;
        route.AddStop(stop);
        return route;
    }
}
