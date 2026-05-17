using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Abstractions;

public interface IGeocodingService
{
    Task<Result<GeoCoordinate>> GeocodeAsync(Address address, CancellationToken ct);
}