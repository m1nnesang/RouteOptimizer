namespace RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password, CancellationToken ct);
    Task<bool> RefreshAsync(CancellationToken ct = default);
    void Logout();
}
