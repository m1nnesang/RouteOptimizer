namespace RouteOptimizer.Driver.Pwa.Models;

public sealed record AuthTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string Token);

public sealed record RequestPasswordResetRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);
