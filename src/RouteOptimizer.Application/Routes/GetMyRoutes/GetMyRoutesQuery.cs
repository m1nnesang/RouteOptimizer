using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Routes.GetRoutes;

namespace RouteOptimizer.Application.Routes.GetMyRoutes;

public record GetMyRoutesQuery() : IQuery<IReadOnlyList<RouteListItemDto>>;
