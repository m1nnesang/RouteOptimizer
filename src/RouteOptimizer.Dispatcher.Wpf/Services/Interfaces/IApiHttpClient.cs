namespace RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

public interface IApiHttpClient
{
    Task<T?> GetAsync<T>(string url, CancellationToken ct = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
}
