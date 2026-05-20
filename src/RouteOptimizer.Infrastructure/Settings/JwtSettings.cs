namespace RouteOptimizer.Infrastructure.Settings;

public sealed class JwtSettings
{
    public string SecretKey { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationHours  { get; init; } = 12;
}
