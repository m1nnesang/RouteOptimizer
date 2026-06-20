namespace RouteOptimizer.Dispatcher.Wpf.Services;

public sealed class DispatcherSettings
{
    public string ApiBaseUrl { get; init; } = "http://localhost:8080";

    public string OsrmUrl { get; init; } = "http://localhost:5000";

    public string RouteHubUrl => $"{ApiBaseUrl.TrimEnd('/')}/hubs/routes";
}
