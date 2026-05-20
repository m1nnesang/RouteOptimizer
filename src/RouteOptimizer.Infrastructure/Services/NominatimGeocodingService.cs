using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Infrastructure.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _client;

    public NominatimGeocodingService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("Nominatim");
    }

    public async Task<Result<GeoCoordinate>> GeocodeAsync(Address address, CancellationToken ct)
    {
        var query = Uri.EscapeDataString($"{address.Street}, {address.City}, {address.PostalCode}, {address.Country}");
        var url = $"search?q={query}&format=json&limit=1";

        var response = await _client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var results = JsonSerializer.Deserialize<List<NominatimResult>>(await response.Content.ReadAsStringAsync(ct));

        if (results is null || results.Count == 0)
            return Result<GeoCoordinate>.Failure("Address not found");


        var lat = double.Parse(results[0].Lat, CultureInfo.InvariantCulture);
        var lon = double.Parse(results[0].Lon, CultureInfo.InvariantCulture);

        var coordinate = GeoCoordinate.Create(lat, lon);
        return coordinate;
    }

    private sealed class NominatimResult
    {
        [JsonPropertyName("lat")] public string Lat { get; init; } = null!;

        [JsonPropertyName("lon")] public string Lon { get; init; } = null!;
    }
}
