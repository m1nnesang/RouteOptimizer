namespace RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

public interface ISessionNotifier
{
    event Action? SessionExpired;
    void NotifySessionExpired();
}
