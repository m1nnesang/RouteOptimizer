using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class SessionNotifier : ISessionNotifier
{
    public event Action? SessionExpired;

    public void NotifySessionExpired() => SessionExpired?.Invoke();
}
