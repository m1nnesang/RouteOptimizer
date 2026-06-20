using System.Security.Claims;
using System.Text.Json;

namespace RouteOptimizer.Driver.Pwa.Auth;

internal static class JwtParser
{
    private const string ExpiryClaim = "exp";

    public static IReadOnlyList<Claim> ParseClaims(string jwt)
    {
        var claims = new List<Claim>();

        var segments = jwt.Split('.');
        if (segments.Length < 2)
            return claims;

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            DecodeBase64Url(segments[1]));

        if (payload is null)
            return claims;

        foreach (var entry in payload)
        {
            var claimType = MapClaimType(entry.Key);

            if (entry.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in entry.Value.EnumerateArray())
                    claims.Add(new Claim(claimType, item.ToString()));
            }
            else
            {
                claims.Add(new Claim(claimType, entry.Value.ToString()));
            }
        }

        return claims;
    }

    public static bool IsExpired(IEnumerable<Claim> claims)
    {
        var expiry = claims.FirstOrDefault(c => c.Type == ExpiryClaim)?.Value;

        if (expiry is null || !long.TryParse(expiry, out var seconds))
            return false;

        return DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
    }

    private static string MapClaimType(string key) =>
        key is "role" or "roles" or ClaimTypes.Role ? ClaimTypes.Role : key;

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = (normalized.Length % 4) switch
        {
            2 => normalized + "==",
            3 => normalized + "=",
            _ => normalized
        };

        return Convert.FromBase64String(normalized);
    }
}
