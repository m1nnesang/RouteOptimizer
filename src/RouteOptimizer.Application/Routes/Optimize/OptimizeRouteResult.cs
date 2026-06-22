namespace RouteOptimizer.Application.Routes.Optimize;

public record OptimizeRouteResult(
    Guid RouteId,
    IReadOnlyList<Guid> OrderedStopIds,
    IReadOnlyList<AlgorithmComparison> Comparisons
);

public record AlgorithmComparison(string AlgorithmName, double TotalDurationSeconds, double ComputationTimeMs);
