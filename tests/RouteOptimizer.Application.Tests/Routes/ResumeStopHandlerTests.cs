using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Stops.ResumeStop;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class ResumeStopHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly ResumeStopCommandHandler _handler;

    public ResumeStopHandlerTests()
    {
        _handler = new ResumeStopCommandHandler(_routeRepository.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new ResumeStopCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RouteNotInProgress_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new ResumeStopCommand(route.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StopNotFound_ThrowsNotFoundException()
    {
        var route = CreateInProgressRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var act = () => _handler.Handle(new ResumeStopCommand(route.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_StopNotSkipped_ReturnsFailure()
    {
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route);
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new ResumeStopCommand(route.Id, stop.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SkippedStop_StopBecomesInProgress()
    {
        var route = CreateInProgressRoute();
        var stop = CreateInProgressStop(route);
        stop.Skip();
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new ResumeStopCommand(route.Id, stop.Id), default);

        result.IsSuccess.Should().BeTrue();
        stop.Status.Should().Be(StopStatus.InProgress);
    }

    private static Route CreateInProgressRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
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
