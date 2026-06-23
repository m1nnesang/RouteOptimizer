using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Routes.GetMyRoutes;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class GetMyRoutesQueryHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetMyRoutesQueryHandler _handler;

    private readonly Guid _driverId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();

    public GetMyRoutesQueryHandlerTests()
    {
        _currentUser.Setup(x => x.UserId).Returns(_driverId);
        _currentUser.Setup(x => x.WarehouseId).Returns(_warehouseId);

        _warehouseRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse>());
        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _handler = new GetMyRoutesQueryHandler(
            _routeRepository.Object,
            _shiftRepository.Object,
            _userRepository.Object,
            _warehouseRepository.Object,
            _currentUser.Object);
    }

    [Fact]
    public async Task Handle_NoActiveShift_ReturnsEmptyList()
    {
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(_driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);

        var result = await _handler.Handle(new GetMyRoutesQuery(), default);

        result.Should().BeEmpty();
        _routeRepository.Verify(
            x => x.GetByAssignedShiftIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveShift_ReturnsRoutesForThatShift()
    {
        var shift = CreateActiveShift();
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(_driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var route = CreateRouteWithStop();
        _routeRepository
            .Setup(x => x.GetByAssignedShiftIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Route> { route });

        var result = await _handler.Handle(new GetMyRoutesQuery(), default);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(route.Id);
        result[0].StopsCount.Should().Be(1);
        result[0].Status.Should().Be(route.Status.ToString());
    }

    [Fact]
    public async Task Handle_QueriesRoutesByActiveShiftId()
    {
        var shift = CreateActiveShift();
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(_driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        _routeRepository
            .Setup(x => x.GetByAssignedShiftIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Route>());

        await _handler.Handle(new GetMyRoutesQuery(), default);

        _routeRepository.Verify(
            x => x.GetByAssignedShiftIdAsync(shift.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExcludesTerminalRoutes()
    {
        var shift = CreateActiveShift();
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(_driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var active = CreateRouteWithStop();
        var completed = CreateCompletedRoute();
        _routeRepository
            .Setup(x => x.GetByAssignedShiftIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Route> { active, completed });

        var result = await _handler.Handle(new GetMyRoutesQuery(), default);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(active.Id);
    }

    private Route CreateCompletedRoute()
    {
        var route = Route.Create(_warehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        route.Complete();
        return route;
    }

    private DriverShift CreateActiveShift()
    {
        var shift = DriverShift.Create(
            _driverId, Guid.NewGuid(), _warehouseId, DateOnly.FromDateTime(DateTime.UtcNow)).Value!;
        shift.Start();
        return shift;
    }

    private Route CreateRouteWithStop()
    {
        var route = Route.Create(_warehouseId).Value!;
        var address = Address.Create("ul. Testowa 1", "Krakow", "30-001", "Poland").Value!;
        var location = GeoCoordinate.Create(50.0647, 19.9450).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, []).Value!;
        route.AddStop(stop);
        return route;
    }
}
