using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.GetRoutes;

public record GetRoutesQuery(Guid? WarehouseId, RouteStatus? Status, int Page = 1, int PageSize = 20) : IQuery<PagedResult<RouteListItemDto>>;
