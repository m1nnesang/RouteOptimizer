namespace RouteOptimizer.Driver.Pwa.Services;

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    RateLimited,
    NetworkError,
    ServerError
}

public interface IAuthService
{
    Task<LoginOutcome> LoginAsync(string email, string password, CancellationToken ct = default);

    Task<bool> RefreshAsync(CancellationToken ct = default);

    Task LogoutAsync();
}
