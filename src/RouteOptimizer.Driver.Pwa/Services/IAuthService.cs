namespace RouteOptimizer.Driver.Pwa.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password, CancellationToken ct = default);

    Task<bool> RefreshAsync(CancellationToken ct = default);

    Task LogoutAsync();
}
