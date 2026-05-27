using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Algorithms;

public class CompositeRouteOptimizer : IRouteOptimizer
{
    public string Name => "Composite";
    private readonly IEnumerable<IRouteOptimizer> _optimizers;

    public CompositeRouteOptimizer(IEnumerable<IRouteOptimizer> optimizers) => _optimizers = optimizers;

    public async Task<RouteOptimizerOutput> OptimizeAsync(RouteOptimizerInput input, CancellationToken ct = default)
    {
        var tasks = _optimizers.Select(o => o.OptimizeAsync(input, ct));

        var result = await Task.WhenAll(tasks);

        return result.MinBy(r => r.TotalDurationSeconds) ?? throw new InvalidOperationException("No routes found");
    }
}
