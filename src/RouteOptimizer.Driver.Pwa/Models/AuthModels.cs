namespace RouteOptimizer.Driver.Pwa.Models;

public sealed record AuthTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string Token);
