using System.Diagnostics;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Algorithms;

public sealed class TwoOptOptimizer : IRouteOptimizer
{
    private const double Epsilon = 1e-9;

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

        var stopwatch = Stopwatch.StartNew();

        var tour = BuildNearestNeighborTour(input.Matrix, input.Stops.Count);

        ImproveWithTwoOpt(tour, input.Matrix, ct);

        var orderedStopIds = tour
            .Skip(1)
            .Select(matrixIndex => input.Stops[matrixIndex - 1].StopId)
            .ToList();

        var totalDuration = TourDuration(tour, input.Matrix);

        stopwatch.Stop();

        return Task.FromResult(new RouteOptimizerOutput
        {
            OrderedStopIds = orderedStopIds,
            TotalDurationSeconds = totalDuration,
            AlgorithmName = Name,
            ComputationTimeMs = stopwatch.Elapsed.TotalMilliseconds
        });
    }

    private static List<int> BuildNearestNeighborTour(DistanceMatrix matrix, int stopCount)
    {
        var visited = new bool[stopCount + 1];
        visited[0] = true;

        var tour = new List<int> { 0 };
        var current = 0;

        for (int step = 0; step < stopCount; step++)
        {
            var bestIndex = -1;
            var bestDuration = double.MaxValue;

            for (int candidate = 1; candidate <= stopCount; candidate++)
            {
                if (visited[candidate])
                    continue;

                var duration = matrix.GetDuration(current, candidate);
                if (duration < bestDuration)
                {
                    bestDuration = duration;
                    bestIndex = candidate;
                }
            }

            visited[bestIndex] = true;
            tour.Add(bestIndex);
            current = bestIndex;
        }

        return tour;
    }

    private static void ImproveWithTwoOpt(List<int> tour, DistanceMatrix matrix, CancellationToken ct)
    {
        var size = tour.Count;
        var improved = true;

        while (improved)
        {
            improved = false;

            for (int i = 1; i < size - 1; i++)
            {
                ct.ThrowIfCancellationRequested();

                for (int j = i + 1; j < size; j++)
                {
                    var a = tour[i - 1];
                    var b = tour[i];
                    var c = tour[j];
                    var d = tour[(j + 1) % size];

                    var currentCost = matrix.GetDuration(a, b) + matrix.GetDuration(c, d);
                    var newCost = matrix.GetDuration(a, c) + matrix.GetDuration(b, d);

                    if (newCost < currentCost - Epsilon)
                    {
                        tour.Reverse(i, j - i + 1);
                        improved = true;
                    }
                }
            }
        }
    }

    private static double TourDuration(List<int> tour, DistanceMatrix matrix)
    {
        double total = 0;

        for (int i = 0; i < tour.Count - 1; i++)
            total += matrix.GetDuration(tour[i], tour[i + 1]);

        total += matrix.GetDuration(tour[^1], tour[0]);

        return total;
    }
}
