using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;

namespace RouteOptimizer.Application.Routes.GetRoutes;

public class GetRoutesQueryHandler : IRequestHandler<GetRoutesQuery, PagedResult<RouteListItemDto>>
{
    private readonly IRouteRepository _routeRepository;

    public GetRoutesQueryHandler(IRouteRepository routeRepository) => _routeRepository = routeRepository;

    public async Task<PagedResult<RouteListItemDto>> Handle(GetRoutesQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (routes, totalCount) = await _routeRepository.GetAllAsync(request.WarehouseId, request.Status, skip, request.PageSize, ct);

        var items = routes.Select(r => new RouteListItemDto(r.Id, r.WarehouseId, r.Status.ToString(), r.Stops.Count, r.AssignedShiftId)).ToList();

        return new PagedResult<RouteListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
