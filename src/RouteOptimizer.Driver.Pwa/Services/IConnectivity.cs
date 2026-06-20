namespace RouteOptimizer.Driver.Pwa.Services;

public interface IConnectivity
{
    bool IsOnline { get; }

    event Func<bool, Task>? Changed;

    Task InitializeAsync();
}
