using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.AssignRoute;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Application.Tests.Routes;

public class AssignRouteCommandHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverShiftRepository> _driverShiftRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AssignRouteCommandHandler _handler;

    public AssignRouteCommandHandlerTests()
    {
        _handler = new AssignRouteCommandHandler(_routeRepository.Object, _driverShiftRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new AssignRouteCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShiftNotFound_ThrowsNotFoundException()
    {
        var route = CreateOptimizedRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _driverShiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);

        var act = () => _handler.Handle(new AssignRouteCommand(route.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShiftWarehouseMismatch_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateOptimizedRoute(warehouseId);
        var shift = DriverShift.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))).Value!;

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _driverShiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new AssignRouteCommand(route.Id, shift.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("warehouse");
    }

    [Fact]
    public async Task Handle_OptimizedRoute_AssignsShiftAndReturnsSuccess()
    {
        var warehouseId = Guid.NewGuid();
        var route = CreateOptimizedRoute(warehouseId);
        var shift = DriverShift.Create(Guid.NewGuid(), Guid.NewGuid(), warehouseId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))).Value!;

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _driverShiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new AssignRouteCommand(route.Id, shift.Id), default);

        result.IsSuccess.Should().BeTrue();
        route.AssignedShiftId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_RouteNotOptimized_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var route = Route.Create(warehouseId).Value!;
        var shift = DriverShift.Create(Guid.NewGuid(), Guid.NewGuid(), warehouseId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))).Value!;

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        _driverShiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new AssignRouteCommand(route.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    private static Route CreateOptimizedRoute(Guid? warehouseId = null)
    {
        var route = Route.Create(warehouseId ?? Guid.NewGuid()).Value!;
        route.Optimize();
        return route;
    }
}
