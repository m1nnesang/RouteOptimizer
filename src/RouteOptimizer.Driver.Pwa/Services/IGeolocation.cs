namespace RouteOptimizer.Driver.Pwa.Services;

public sealed record GeoPosition(double Latitude, double Longitude);

public interface IGeolocation
{
    Task<GeoPosition?> TryGetCurrentPositionAsync(CancellationToken ct = default);
}
