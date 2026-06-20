using Microsoft.AspNetCore.SignalR.Client;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class RouteHubService : IRouteHubService
{
    private const string HubUrl = "http://localhost:8080/hubs/routes";

    private const string RouteStartedEventName = "RouteStarted";
    private const string StopCompletedEventName = "StopCompleted";
    private const string StopFailedEventName = "StopFailed";
    private const string StopSkippedEventName = "StopSkipped";
    private const string RouteChangedEventName = "RouteChanged";
    private const string DriverLocationEventName = "DriverLocation";

    private readonly TokenStorage _tokenStorage;
    private HubConnection? _connection;

    public RouteHubService(TokenStorage tokenStorage) => _tokenStorage = tokenStorage;

    public event Action<RouteStartedEvent>? RouteStarted;
    public event Action<StopEvent>? StopCompleted;
    public event Action<StopEvent>? StopFailed;
    public event Action<StopEvent>? StopSkipped;
    public event Action<RouteChangedEvent>? RouteChanged;
    public event Action<DriverLocationEvent>? DriverLocation;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
            return;

        var connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_tokenStorage.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<RouteStartedEvent>(RouteStartedEventName, e => RouteStarted?.Invoke(e));
        connection.On<StopEvent>(StopCompletedEventName, e => StopCompleted?.Invoke(e));
        connection.On<StopEvent>(StopFailedEventName, e => StopFailed?.Invoke(e));
        connection.On<StopEvent>(StopSkippedEventName, e => StopSkipped?.Invoke(e));
        connection.On<RouteChangedEvent>(RouteChangedEventName, e => RouteChanged?.Invoke(e));
        connection.On<DriverLocationEvent>(DriverLocationEventName, e => DriverLocation?.Invoke(e));

        connection.Reconnected += _ => connection.InvokeAsync("JoinWarehouse");

        await connection.StartAsync(ct);
        await connection.InvokeAsync("JoinWarehouse", ct);

        _connection = connection;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_connection is null)
            return;

        var connection = _connection;
        _connection = null;

        await connection.StopAsync(ct);
        await connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }
}
