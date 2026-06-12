using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.CompleteRoute;

public record CompleteRouteCommand(Guid RouteId) : ICommand<Result>;
