namespace RouteOptimizer.Driver.Pwa.Services;

public interface IRouteHubClient : IAsyncDisposable
{
    Task ConnectAsync(Guid routeId, Func<Task> onRouteChanged, CancellationToken ct = default);

    Task StopAsync();
}
