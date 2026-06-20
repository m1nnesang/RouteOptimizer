using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public interface IOfflineStore
{
    Task SaveRouteAsync(RouteDetail route);

    Task<RouteDetail?> GetRouteAsync(Guid id);

    Task SaveRoutesAsync(IReadOnlyList<RouteListItem> routes);

    Task<IReadOnlyList<RouteListItem>> GetRoutesAsync();

    Task<IReadOnlyList<OutboxItem>> GetOutboxAsync();

    Task EnqueueAsync(OutboxItem item);

    Task RemoveAsync(Guid itemId);

    Task<int> CountAsync();
}
