using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;
using RouteOptimizer.Infrastructure.Services;

namespace RouteOptimizer.Infrastructure.Tests.Services;

public class CachedGeocodingServiceTests
{
    private readonly Mock<IGeocodingService> _inner = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly CachedGeocodingService _sut;

    public CachedGeocodingServiceTests()
    {
        _sut = new CachedGeocodingService(
            _inner.Object, _cache.Object, NullLogger<CachedGeocodingService>.Instance);
    }

    [Fact]
    public async Task GeocodeAsync_CacheHit_ReturnsCachedAndSkipsInner()
    {
        var address = CreateAddress();
        var json = JsonSerializer.Serialize(new { Lat = 52.2297, Lon = 21.0122 });
        _cache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        var result = await _sut.GeocodeAsync(address, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Latitude.Should().Be(52.2297);
        result.Value!.Longitude.Should().Be(21.0122);
        _inner.Verify(
            x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GeocodeAsync_CacheMiss_CallsInnerAndStores()
    {
        var address = CreateAddress();
        _cache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        var coordinate = GeoCoordinate.Create(52.0, 21.0).Value!;
        _inner
            .Setup(x => x.GeocodeAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(coordinate));

        var result = await _sut.GeocodeAsync(address, default);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GeocodeAsync_InnerFailure_DoesNotCache()
    {
        var address = CreateAddress();
        _cache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _inner
            .Setup(x => x.GeocodeAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Failure("not found"));

        var result = await _sut.GeocodeAsync(address, default);

        result.IsFailure.Should().BeTrue();
        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GeocodeAsync_EqualAddresses_ResolveToSameCacheKey()
    {
        var keys = new List<string>();
        _cache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((k, _) => keys.Add(k))
            .ReturnsAsync((byte[]?)null);
        var coordinate = GeoCoordinate.Create(52.0, 21.0).Value!;
        _inner
            .Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(coordinate));

        await _sut.GeocodeAsync(CreateAddress(), default);
        await _sut.GeocodeAsync(CreateAddress(), default);

        keys.Should().HaveCount(2);
        keys[1].Should().Be(keys[0]);
        keys[0].Should().StartWith("geocode:");
    }

    private static Address CreateAddress() =>
        Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
}
