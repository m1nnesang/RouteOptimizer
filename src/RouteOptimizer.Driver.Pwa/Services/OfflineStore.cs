using Blazored.LocalStorage;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class OfflineStore : IOfflineStore
{
    private const string RoutePrefix = "ro_cache_route_";
    private const string RoutesKey = "ro_cache_routes";
    private const string OutboxKey = "ro_outbox";

    private readonly ILocalStorageService _storage;

    public OfflineStore(ILocalStorageService storage) => _storage = storage;

    public async Task SaveRouteAsync(RouteDetail route) =>
        await _storage.SetItemAsync(RoutePrefix + route.Id, route);

    public async Task<RouteDetail?> GetRouteAsync(Guid id) =>
        await _storage.GetItemAsync<RouteDetail>(RoutePrefix + id);

    public async Task SaveRoutesAsync(IReadOnlyList<RouteListItem> routes) =>
        await _storage.SetItemAsync(RoutesKey, routes);

    public async Task<IReadOnlyList<RouteListItem>> GetRoutesAsync() =>
        await _storage.GetItemAsync<IReadOnlyList<RouteListItem>>(RoutesKey) ?? [];

    public async Task<IReadOnlyList<OutboxItem>> GetOutboxAsync() =>
        await _storage.GetItemAsync<List<OutboxItem>>(OutboxKey) ?? [];

    public async Task EnqueueAsync(OutboxItem item)
    {
        var items = (await GetOutboxAsync()).ToList();
        items.Add(item);
        await _storage.SetItemAsync(OutboxKey, items);
    }

    public async Task RemoveAsync(Guid itemId)
    {
        var items = (await GetOutboxAsync()).Where(i => i.Id != itemId).ToList();
        await _storage.SetItemAsync(OutboxKey, items);
    }

    public async Task<int> CountAsync() => (await GetOutboxAsync()).Count;
}
