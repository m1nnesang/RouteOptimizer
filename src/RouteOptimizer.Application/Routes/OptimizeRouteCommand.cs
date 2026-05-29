using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Routes;

public record OptimizeRouteCommand(Guid RouteId, DateOnly RouteDate) : ICommand<OptimizeRouteResult>;
