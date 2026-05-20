namespace RouteOptimizer.Application.Auth;

public sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);