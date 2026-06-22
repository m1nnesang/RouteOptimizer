namespace RouteOptimizer.Application.Models;

public sealed class RouteOptimizerOutput
{
    public required IReadOnlyList<Guid> OrderedStopIds { get; init; }
    public required double TotalDurationSeconds { get; init; }
    public required string AlgorithmName { get; init; }
    public double ComputationTimeMs { get; init; }
}
