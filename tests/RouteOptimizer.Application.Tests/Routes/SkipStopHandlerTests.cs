using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Stops.SkipStop;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class SkipStopHandlerTests
{
    private static readonly Guid DriverId = Guid.NewGuid();

    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly SkipStopHandler _handler;

    public SkipStopHandlerTests()
    {
        _currentUser.SetupGet(x => x.UserId).Returns(DriverId);
        _shiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverShift.Create(DriverId, Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow)).Value!);

        _handler = new SkipStopHandler(_routeRepository.Object, _shiftRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new SkipStopCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RouteNotInProgress_ReturnsFailure()
    {
        var route = CreateAssignedRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new SkipStopCommand(route.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StopNotFound_ThrowsNotFoundException()
    {
        var route = CreateInProgressRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var act = () => _handler.Handle(new SkipStopCommand(route.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_StopAlreadyCompleted_ReturnsFailure()
    {
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route);
        stop.Complete();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new SkipStopCommand(route.Id, stop.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PendingStop_StopBecomesSkipped()
    {
        var route = CreateInProgressRoute();
        var stop = CreatePendingStop(route);
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new SkipStopCommand(route.Id, stop.Id), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.Skipped);
    }

    [Fact]
    public async Task Handle_InProgressStop_StopBecomesSkipped()
    {
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route);
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new SkipStopCommand(route.Id, stop.Id), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.Skipped);
    }

    private static Route CreateAssignedRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        return route;
    }

    private static Route CreateInProgressRoute()
    {
        var route = CreateAssignedRoute();
        route.Start();
        return route;
    }

    private static Stop CreatePendingStop(Route route)
    {
        var address = Address.Create("ul. Marszalkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, []).Value!;
        route.AddStop(stop);
        return stop;
    }

    private static Stop CreateInProgressStop(Route route)
    {
        var stop = CreatePendingStop(route);
        stop.Start();
        return stop;
    }
}
