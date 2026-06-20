using Microsoft.JSInterop;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class GeolocationService : IGeolocation
{
    private const int TimeoutMs = 10_000;

    private readonly IJSRuntime _js;

    public GeolocationService(IJSRuntime js) => _js = js;

    public async Task<GeoPosition?> TryGetCurrentPositionAsync(CancellationToken ct = default)
    {
        try
        {
            return await _js.InvokeAsync<GeoPosition>("driverGeolocation.getCurrentPosition", ct, TimeoutMs);
        }
        catch (JSException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
