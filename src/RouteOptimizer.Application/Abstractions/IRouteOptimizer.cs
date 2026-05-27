using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Abstractions;

public interface IRouteOptimizer
{
    string Name { get;}
    Task<RouteOptimizerOutput> OptimizeAsync(RouteOptimizerInput input, CancellationToken ct = default);
}
