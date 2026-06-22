namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class OptimizeResult
{
    public Guid RouteId { get; set; }
    public List<Guid> OrderedStopIds { get; set; } = [];
    public List<AlgorithmComparison> Comparisons { get; set; } = [];
}

public class AlgorithmComparison
{
    public string AlgorithmName { get; set; } = string.Empty;
    public double TotalDurationSeconds { get; set; }
    public double ComputationTimeMs { get; set; }
}
