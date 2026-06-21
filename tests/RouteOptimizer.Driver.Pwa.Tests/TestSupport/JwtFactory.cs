using System.Text;
using System.Text.Json;

namespace RouteOptimizer.Driver.Pwa.Tests.TestSupport;

public static class JwtFactory
{
    public static string Create(object payload)
    {
        var header = Segment(new { alg = "none", typ = "JWT" });
        var body = Segment(payload);
        return $"{header}.{body}.signature";
    }

    public static string Expired() =>
        Create(new
        {
            email = "driver@example.com",
            role = "Driver",
            exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
        });

    public static string Valid() =>
        Create(new
        {
            email = "driver@example.com",
            role = "Driver",
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        });

    private static string Segment(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
