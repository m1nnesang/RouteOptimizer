using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Routes.Optimize;

public record OptimizeRouteCommand(Guid RouteId, DateOnly RouteDate) : ICommand<OptimizeRouteResult>;
