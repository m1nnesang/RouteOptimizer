using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public interface IMapInterop
{
    Task InitAsync(string elementId, CancellationToken ct = default);

    Task RenderAsync(IReadOnlyList<RouteStop> stops, Guid? currentStopId,
        IReadOnlyList<GeoPoint>? geometry = null, GeoPoint? warehouse = null, CancellationToken ct = default);

    Task FocusAsync(double latitude, double longitude, CancellationToken ct = default);

    Task FitAllAsync(IReadOnlyList<RouteStop> stops, CancellationToken ct = default);

    Task SetDriverAsync(double latitude, double longitude, CancellationToken ct = default);

    Task DisposeMapAsync(CancellationToken ct = default);
}
