using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.InterruptRoute;

public record InterruptRouteCommand(Guid RouteId) : ICommand<Result>;
