using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Infrastructure.Services;

public sealed class OsrmRouteGeometryProvider : IRouteGeometryProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OsrmRouteGeometryProvider> _logger;

    public OsrmRouteGeometryProvider(IHttpClientFactory httpClientFactory, ILogger<OsrmRouteGeometryProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(double Lat, double Lon)>> GetRouteAsync(
        IReadOnlyList<(double Lat, double Lon)> waypoints,
        CancellationToken ct = default)
    {
        if (waypoints.Count < 2)
            return [];

        var coordsString = string.Join(";", waypoints.Select(w => FormatCoord(w.Lon, w.Lat)));
        var url = $"/route/v1/driving/{coordsString}?overview=full&geometries=geojson";

        try
        {
            var client = _httpClientFactory.CreateClient("Osrm");
            var response = await client.GetFromJsonAsync<OsrmRouteResponse>(url, ct);

            var coordinates = response?.Routes?.FirstOrDefault()?.Geometry?.Coordinates;

            if (coordinates is null)
                return [];

            return coordinates
                .Where(c => c.Length >= 2)
                .Select(c => (Lat: c[1], Lon: c[0]))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch route geometry from OSRM; falling back to straight lines");
            return [];
        }
    }

    private static string FormatCoord(double lon, double lat) =>
        $"{lon.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)}";

    private sealed record OsrmRouteResponse(
        [property: JsonPropertyName("routes")] OsrmRoute[]? Routes
    );

    private sealed record OsrmRoute(
        [property: JsonPropertyName("geometry")] OsrmGeometry? Geometry
    );

    private sealed record OsrmGeometry(
        [property: JsonPropertyName("coordinates")] double[][]? Coordinates
    );
}
