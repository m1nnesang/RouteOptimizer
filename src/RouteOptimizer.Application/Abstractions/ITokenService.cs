using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string HashToken(string rawToken);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    TimeSpan RefreshTokenExpiration { get; }

}
