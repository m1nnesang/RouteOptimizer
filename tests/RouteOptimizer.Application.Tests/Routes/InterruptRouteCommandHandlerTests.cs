using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.InterruptRoute;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Application.Tests.Routes;

public class InterruptRouteCommandHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly InterruptRouteCommandHandler _handler;

    public InterruptRouteCommandHandlerTests()
    {
        _handler = new InterruptRouteCommandHandler(_routeRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new InterruptRouteCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InProgressRoute_InterruptsAndReturnsSuccess()
    {
        var route = CreateInProgressRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new InterruptRouteCommand(route.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RouteNotInProgress_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new InterruptRouteCommand(route.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    private static Route CreateInProgressRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        return route;
    }
}
