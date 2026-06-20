using RouteOptimizer.Dispatcher.Wpf.Models;

namespace RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

public interface IRouteHubService : IAsyncDisposable
{
    event Action<RouteStartedEvent>? RouteStarted;
    event Action<StopEvent>? StopCompleted;
    event Action<StopEvent>? StopFailed;
    event Action<StopEvent>? StopSkipped;
    event Action<RouteChangedEvent>? RouteChanged;
    event Action<DriverLocationEvent>? DriverLocation;

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
