using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Algorithms;

public sealed class TwoOptOptimizer : IRouteOptimizer
{
    public string Name => "Two Opt";

    public Task<RouteOptimizerOutput> OptimizeAsync(RouteOptimizerInput input, CancellationToken ct = default)
    {
        if (input.Stops.Count == 0)
            return Task.FromResult(new RouteOptimizerOutput
            {
                OrderedStopIds = [],
                TotalDurationSeconds = 0,
                AlgorithmName = Name
            });

        if (input.Matrix.Size != input.Stops.Count + 1)
            throw new InvalidOperationException("Matrix size does not match number of stops");

        List<Guid> orderedStopIds = [];
        double totalDurationSeconds = 0;

        var route = Enumerable.Range(0, input.Stops.Count)
            .ToList();

        bool improved = true;

        while (improved)
        {
            improved = false;
            for (int i = 0; i <= route.Count - 2; i++)
            {
                ct.ThrowIfCancellationRequested();

                for (int j = i + 2; j <= route.Count - 1; j++)
                {
                    var rightEnd = j == route.Count - 1 ? 0 : route[j + 1] + 1;

                    var currentCost = input.Matrix.GetDuration(route[i] + 1, route[i + 1] + 1)
                                      + input.Matrix.GetDuration(route[j] + 1, rightEnd);

                    var newCost = input.Matrix.GetDuration(route[i] + 1, route[j] + 1)
                                  + input.Matrix.GetDuration(route[i + 1] + 1, rightEnd);

                    if (newCost < currentCost)
                    {
                        route.Reverse(i + 1, j - i);
                        improved = true;
                    }
                }
            }
        }

        foreach (var stopIndex in route )
        {
            orderedStopIds.Add(input.Stops[stopIndex].StopId);
        }

        for (int i = 0; i < route.Count - 1; i++)
        {
            totalDurationSeconds += input.Matrix.GetDuration(route[i] + 1, route[i + 1] + 1);
        }

        totalDurationSeconds += input.Matrix.GetDuration(0, route[0] + 1);
        totalDurationSeconds += input.Matrix.GetDuration(route[route.Count - 1] + 1, 0);

        var output = new RouteOptimizerOutput
        {
            OrderedStopIds = orderedStopIds,
            TotalDurationSeconds = totalDurationSeconds,
            AlgorithmName = Name
        };

        return Task.FromResult(output);
    }
}
