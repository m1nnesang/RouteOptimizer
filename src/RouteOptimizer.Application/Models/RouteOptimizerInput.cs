namespace RouteOptimizer.Application.Models;

public sealed class RouteOptimizerInput
{
    public required (double lat, double lon) Warehouse { get; init; }
    public required IReadOnlyList<StopInput> Stops { get; init; }
    public required DistanceMatrix Matrix { get; init; }
    public required DateOnly RouteDate { get; init; }
}
