using RouteOptimizer.Driver.Pwa.Models;
using RouteOptimizer.Driver.Pwa.Services;

namespace RouteOptimizer.Driver.Pwa.Tests.TestSupport;

public sealed class InMemoryOfflineStore : IOfflineStore
{
    private readonly Dictionary<Guid, RouteDetail> _routes = [];
    private readonly List<OutboxItem> _outbox = [];
    private IReadOnlyList<RouteListItem> _list = [];

    public IReadOnlyList<OutboxItem> Outbox => _outbox;

    public Task SaveRouteAsync(RouteDetail route)
    {
        _routes[route.Id] = route;
        return Task.CompletedTask;
    }

    public Task<RouteDetail?> GetRouteAsync(Guid id) =>
        Task.FromResult(_routes.GetValueOrDefault(id));

    public Task SaveRoutesAsync(IReadOnlyList<RouteListItem> routes)
    {
        _list = routes;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RouteListItem>> GetRoutesAsync() => Task.FromResult(_list);

    public Task<IReadOnlyList<OutboxItem>> GetOutboxAsync() =>
        Task.FromResult<IReadOnlyList<OutboxItem>>(_outbox.ToList());

    public Task EnqueueAsync(OutboxItem item)
    {
        _outbox.Add(item);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid itemId)
    {
        _outbox.RemoveAll(i => i.Id == itemId);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync() => Task.FromResult(_outbox.Count);
}

public sealed class FakeConnectivity : IConnectivity
{
    public FakeConnectivity(bool isOnline) => IsOnline = isOnline;

    public bool IsOnline { get; set; }

    public event Func<bool, Task>? Changed;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task RaiseChangedAsync(bool isOnline)
    {
        IsOnline = isOnline;
        return Changed?.Invoke(isOnline) ?? Task.CompletedTask;
    }
}
