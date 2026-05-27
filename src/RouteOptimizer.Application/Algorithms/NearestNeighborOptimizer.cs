using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Algorithms;

public sealed class NearestNeighborOptimizer : IRouteOptimizer
{
    public string Name => "Nearest Neighbor";

    public Task<RouteOptimizerOutput> OptimizeAsync(RouteOptimizerInput input, CancellationToken ct = default)
    {
        List<Guid> orderedStopIds = new List<Guid>();
        double totalDurationSeconds = 0;

        bool[] visited = new bool[input.Stops.Count];

        var currentIndex = 0;

        for (int i = 0; i < input.Stops.Count; i++)
        {
            var bestIndex = -1;
            var bestDuration = double.MaxValue;

            for (int j = 0; j < input.Stops.Count; j++)
            {
                if (!visited[j])
                {
                    var duration = input.Matrix.GetDuration(currentIndex, j + 1);

                    if (duration < bestDuration)
                    {
                        bestIndex = j;
                        bestDuration = duration;
                    }
                }
            }
            visited[bestIndex] = true;
            orderedStopIds.Add(input.Stops[bestIndex].StopId);
            totalDurationSeconds += bestDuration;
            currentIndex = bestIndex + 1;
        }

        totalDurationSeconds += input.Matrix.GetDuration(currentIndex, 0);

        var output = new RouteOptimizerOutput
        {
            OrderedStopIds = orderedStopIds,
            TotalDurationSeconds = totalDurationSeconds,
            AlgorithmName = Name
        };

        return Task.FromResult(output);
    }
}
