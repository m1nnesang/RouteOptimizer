using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Infrastructure.Services;

public class CachedGeocodingService : IGeocodingService
{
    private readonly IGeocodingService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedGeocodingService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    public CachedGeocodingService(
        IGeocodingService inner,
        IDistributedCache cache,
        ILogger<CachedGeocodingService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<GeoCoordinate>> GeocodeAsync(Address address, CancellationToken ct)
    {
        var key = BuildKey(address);

        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Geocode cache hit {Key}", key);
                var dto = JsonSerializer.Deserialize<CoordinateDto>(cached);
                return GeoCoordinate.Create(dto!.Lat, dto.Lon);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable on cache read — falling back to geocoding service");
        }

        var result = await _inner.GeocodeAsync(address, ct);
        if (result.IsFailure)
            return result;

        try
        {
            var payload = JsonSerializer.Serialize(
                new CoordinateDto(result.Value!.Latitude, result.Value!.Longitude));
            await _cache.SetStringAsync(key, payload, CacheOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable on cache write — result not cached");
        }

        return result;
    }

        private static string BuildKey(Address address)
        {
            var raw = $"{address.Street}|{address.City}|{address.PostalCode}|{address.Country}"
                .Trim().ToLowerInvariant();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
            return $"geocode:{hash}";
        }

    private sealed record CoordinateDto(double Lat, double Lon);
}
