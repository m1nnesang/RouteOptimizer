using Microsoft.AspNetCore.SignalR.Client;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class RouteHubClient : IRouteHubClient
{
    private const string RouteChangedEvent = "RouteChanged";
    private const string JoinRouteMethod = "JoinRoute";

    private readonly string _hubUrl;
    private readonly ITokenStore _tokenStore;

    private HubConnection? _connection;
    private Guid _routeId;

    public RouteHubClient(string hubUrl, ITokenStore tokenStore)
    {
        _hubUrl = hubUrl;
        _tokenStore = tokenStore;
    }

    public async Task ConnectAsync(Guid routeId, Func<Task> onRouteChanged, CancellationToken ct = default)
    {
        if (_connection is not null)
            return;

        _routeId = routeId;

        var connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
                options.AccessTokenProvider = async () => await _tokenStore.GetAccessTokenAsync())
            .WithAutomaticReconnect()
            .Build();

        connection.On<RouteChangedPayload>(RouteChangedEvent, async _ => await onRouteChanged());

        connection.Reconnected += async _ =>
        {
            try
            {
                await connection.InvokeAsync(JoinRouteMethod, _routeId);
                await onRouteChanged();
            }
            catch (Exception)
            {
            }
        };

        try
        {
            await connection.StartAsync(ct);
            await connection.InvokeAsync(JoinRouteMethod, routeId, ct);
            _connection = connection;
        }
        catch (Exception)
        {
            await connection.DisposeAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_connection is null)
            return;

        var connection = _connection;
        _connection = null;

        try
        {
            await connection.StopAsync();
        }
        catch (Exception)
        {
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private sealed record RouteChangedPayload(Guid RouteId);
}
