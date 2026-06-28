using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.CancelRoute;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Application.Tests.Routes;

public class CancelRouteCommandHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly CancelRouteCommandHandler _handler;

    public CancelRouteCommandHandlerTests()
    {
        _orderRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _handler = new CancelRouteCommandHandler(_routeRepository.Object, _orderRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new CancelRouteCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PendingRoute_CancelsAndReturnsSuccess()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new CancelRouteCommand(route.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CompletedRoute_ReturnsFailure()
    {
        var route = CreateCompletedRoute();
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new CancelRouteCommand(route.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    private static Route CreateCompletedRoute()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        route.Complete();
        return route;
    }
}
