using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.GetRouteById;

public record GetRouteByIdQuery(Guid RouteId) : IQuery<Result<RouteDto>>;
