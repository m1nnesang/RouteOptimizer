using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.CancelRoute;

public record CancelRouteCommand(Guid RouteId) : ICommand<Result>;
