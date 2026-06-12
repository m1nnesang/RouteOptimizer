using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.GetRouteById;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Routes;

public class GetRouteByIdQueryHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly GetRouteByIdQueryHandler _handler;

    public GetRouteByIdQueryHandlerTests()
    {
        _handler = new GetRouteByIdQueryHandler(_routeRepository.Object);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var act = () => _handler.Handle(new GetRouteByIdQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RouteFound_ReturnsDtoWithCorrectFields()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new GetRouteByIdQuery(route.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(route.Id);
        result.Value.WarehouseId.Should().Be(route.WarehouseId);
        result.Value.Status.Should().Be(route.Status.ToString());
        result.Value.AssignedShiftId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RouteFound_StopsAreMappedCorrectly()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;
        var address = Address.Create("ul. Marszalkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        var stop = Stop.Create(route.Id, address, location, null, 0, []).Value!;
        route.AddStop(stop);

        _routeRepository
            .Setup(x => x.GetByIdAsync(route.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);

        var result = await _handler.Handle(new GetRouteByIdQuery(route.Id), default);

        var stopDto = result.Value!.Stops.Single();
        stopDto.Id.Should().Be(stop.Id);
        stopDto.City.Should().Be("Warszawa");
        stopDto.Street.Should().Be("ul. Marszalkowska 1");
        stopDto.Latitude.Should().Be(52.2297);
        stopDto.Longitude.Should().Be(21.0122);
        stopDto.Status.Should().Be(stop.Status.ToString());
    }
}
