using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Stops;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.GetRouteById;

public class GetRouteByIdQueryHandler : IRequestHandler<GetRouteByIdQuery, Result<RouteDto>>
{
    private readonly IRouteRepository _routeRepository;
    private readonly ICurrentUser _currentUser;

    public GetRouteByIdQueryHandler(IRouteRepository routeRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<RouteDto>> Handle(GetRouteByIdQuery request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route is not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route is not found");

        var stops = route.Stops.Select(s => new StopDto(s.Id, s.Sequence, s.Address.City, s.Address.Street,
            s.Location.Latitude, s.Location.Longitude, s.Status.ToString(), s.Orders)).ToList();

        var routeDto = new RouteDto(
            route.Id,
            route.WarehouseId,
            route.AssignedShiftId,
            route.Status.ToString(),
            stops
        );

        return Result<RouteDto>.Success(routeDto);
    }
}
