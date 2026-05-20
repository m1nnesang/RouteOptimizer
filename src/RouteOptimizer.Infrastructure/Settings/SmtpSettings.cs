namespace RouteOptimizer.Infrastructure.Settings;

public sealed class SmtpSettings
{
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string From { get; init; } = null!;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
}