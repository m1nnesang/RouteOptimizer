using System.Net.Http.Json;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class RouteApiClient : IRouteApi
{
    private readonly HttpClient _http;

    public RouteApiClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<RouteListItem>> GetMyRoutesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<RouteListItem>>("api/routes/mine", ct) ?? [];

    public Task<RouteDetail?> GetRouteAsync(Guid id, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<RouteDetail>($"api/routes/{id}", ct);
}
