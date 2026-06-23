namespace RouteOptimizer.Application.Abstractions;

public interface IRouteGeometryProvider
{
    Task<IReadOnlyList<(double Lat, double Lon)>> GetRouteAsync(
        IReadOnlyList<(double Lat, double Lon)> waypoints,
        CancellationToken ct = default);
}
