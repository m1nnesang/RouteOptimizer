namespace RouteOptimizer.Application.Models;

public sealed class DistanceMatrix
{
    private readonly double[,] _durations;

    public int Size => _durations.GetLength(0);

    public DistanceMatrix(double[,] durations) => _durations = durations;

    public double GetDuration(int from, int to) => _durations[from, to];
}
