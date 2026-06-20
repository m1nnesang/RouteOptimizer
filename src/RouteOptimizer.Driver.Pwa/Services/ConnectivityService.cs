using Microsoft.JSInterop;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class ConnectivityService : IConnectivity, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<ConnectivityService>? _ref;
    private bool _initialized;

    public ConnectivityService(IJSRuntime js) => _js = js;

    public bool IsOnline { get; private set; } = true;

    public event Func<bool, Task>? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        _ref = DotNetObjectReference.Create(this);

        try
        {
            IsOnline = await _js.InvokeAsync<bool>("driverConnectivity.isOnline");
            await _js.InvokeVoidAsync("driverConnectivity.register", _ref);
        }
        catch (Exception)
        {
        }
    }

    [JSInvokable]
    public async Task OnConnectivityChanged(bool isOnline)
    {
        IsOnline = isOnline;

        if (Changed is not null)
            await Changed.Invoke(isOnline);
    }

    public ValueTask DisposeAsync()
    {
        _ref?.Dispose();
        return ValueTask.CompletedTask;
    }
}
