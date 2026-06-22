using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Geocoding;
using RouteOptimizer.Infrastructure.Settings;

namespace RouteOptimizer.Infrastructure.Services;

public class PhotonAddressAutocompleteService : IAddressAutocompleteService
{
    private const int MinQueryLength = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly GeocodingSettings _settings;

    public PhotonAddressAutocompleteService(IHttpClientFactory factory, IOptions<GeocodingSettings> settings)
    {
        _client = factory.CreateClient("Photon");
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MinQueryLength)
            return [];

        var url = $"api?q={Uri.EscapeDataString(query.Trim())}&limit=6&lang=en" +
                  $"&lat={_settings.BiasLatitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&lon={_settings.BiasLongitude.ToString(CultureInfo.InvariantCulture)}";

        var response = await _client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var collection = JsonSerializer.Deserialize<PhotonFeatureCollection>(
            await response.Content.ReadAsStringAsync(ct), JsonOptions);

        if (collection?.Features is null)
            return [];

        return collection.Features
            .Where(f => f.Geometry?.Coordinates is { Length: 2 })
            .Where(f => string.Equals(f.Properties?.CountryCode, "PL", StringComparison.OrdinalIgnoreCase))
            .Select(ToSuggestion)
            .ToList();
    }

    private static AddressSuggestion ToSuggestion(PhotonFeature feature)
    {
        var props = feature.Properties!;
        var lon = feature.Geometry!.Coordinates![0];
        var lat = feature.Geometry.Coordinates![1];

        var street = props.Street ?? props.Name ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(props.HouseNumber))
            street = $"{street} {props.HouseNumber}".Trim();

        var city = props.City ?? props.District ?? props.County ?? props.State ?? string.Empty;
        var postcode = props.Postcode ?? string.Empty;
        var country = props.Country ?? "Poland";

        var label = string.Join(", ", new[]
            {
                street,
                string.Join(" ", new[] { postcode, city }.Where(p => !string.IsNullOrWhiteSpace(p))),
            }
            .Where(p => !string.IsNullOrWhiteSpace(p)));

        return new AddressSuggestion(label, street, city, postcode, country, lat, lon);
    }

    private sealed class PhotonFeatureCollection
    {
        public List<PhotonFeature>? Features { get; init; }
    }

    private sealed class PhotonFeature
    {
        public PhotonGeometry? Geometry { get; init; }
        public PhotonProperties? Properties { get; init; }
    }

    private sealed class PhotonGeometry
    {
        public double[]? Coordinates { get; init; }
    }

    private sealed class PhotonProperties
    {
        public string? Name { get; init; }
        public string? Street { get; init; }

        [JsonPropertyName("housenumber")]
        public string? HouseNumber { get; init; }

        public string? Postcode { get; init; }
        public string? City { get; init; }
        public string? District { get; init; }
        public string? County { get; init; }
        public string? State { get; init; }
        public string? Country { get; init; }

        [JsonPropertyName("countrycode")]
        public string? CountryCode { get; init; }
    }
}
