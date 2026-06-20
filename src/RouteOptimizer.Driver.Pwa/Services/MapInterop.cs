using Microsoft.JSInterop;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class MapInterop : IMapInterop
{
    private readonly IJSRuntime _js;

    public MapInterop(IJSRuntime js) => _js = js;

    public Task InitAsync(string elementId, CancellationToken ct = default) =>
        _js.InvokeVoidAsync("driverMap.init", ct, elementId).AsTask();

    public Task RenderAsync(IReadOnlyList<RouteStop> stops, Guid? currentStopId, CancellationToken ct = default)
    {
        var payload = stops.Select(s => new MapStop(s.Id, s.Sequence, s.Latitude, s.Longitude, s.Status)).ToList();
        return _js.InvokeVoidAsync("driverMap.render", ct, payload, currentStopId).AsTask();
    }

    public Task FocusAsync(double latitude, double longitude, CancellationToken ct = default) =>
        _js.InvokeVoidAsync("driverMap.focus", ct, latitude, longitude).AsTask();

    public Task FitAllAsync(IReadOnlyList<RouteStop> stops, CancellationToken ct = default)
    {
        var payload = stops.Select(s => new MapStop(s.Id, s.Sequence, s.Latitude, s.Longitude, s.Status)).ToList();
        return _js.InvokeVoidAsync("driverMap.fitAll", ct, payload).AsTask();
    }

    public Task SetDriverAsync(double latitude, double longitude, CancellationToken ct = default) =>
        _js.InvokeVoidAsync("driverMap.setDriver", ct, latitude, longitude).AsTask();

    public Task DisposeMapAsync(CancellationToken ct = default) =>
        _js.InvokeVoidAsync("driverMap.dispose", ct).AsTask();

    private sealed record MapStop(Guid Id, int Sequence, double Latitude, double Longitude, string Status);
}
