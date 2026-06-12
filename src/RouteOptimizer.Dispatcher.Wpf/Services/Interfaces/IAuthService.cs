namespace RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password, CancellationToken ct);
    void Logout();
}
