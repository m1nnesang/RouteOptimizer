using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public interface IRouteApi
{
    Task<IReadOnlyList<RouteListItem>> GetMyRoutesAsync(CancellationToken ct = default);

    Task<RouteDetail?> GetRouteAsync(Guid id, CancellationToken ct = default);
}
