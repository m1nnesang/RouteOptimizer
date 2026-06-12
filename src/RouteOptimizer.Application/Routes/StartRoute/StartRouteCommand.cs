using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.StartRoute;

public record StartRouteCommand(Guid RouteId) : ICommand<Result>;
