namespace RouteOptimizer.Driver.Pwa.Services;

public interface IOutboxFlusher
{
    Task<int> FlushAsync(CancellationToken ct = default);

    Task<int> PendingCountAsync();
}
