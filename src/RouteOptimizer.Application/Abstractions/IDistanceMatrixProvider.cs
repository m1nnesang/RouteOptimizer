using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Abstractions;

public interface IDistanceMatrixProvider
{
    Task<DistanceMatrix> GetMatrixAsync((double Lat , double Lon) warehouse,
        IReadOnlyList<(double Lat, double Lon)> input,
        CancellationToken ct = default);
}
