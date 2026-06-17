using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;
using RouteOptimizer.Infrastructure.Services;
using RouteOptimizer.Infrastructure.Settings;

namespace RouteOptimizer.Infrastructure.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;
    private readonly JwtSettings _settings = new()
    {
        SecretKey = "test-secret-key-that-is-long-enough-for-hmac256",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpirationMinutes = 60,
        RefreshTokenExpirationHours = 12
    };

    public JwtTokenServiceTests() =>
        _sut = new JwtTokenService(Options.Create(_settings));

    [Fact]
    public void GenerateAccessToken_ContainsSubEmailAndRoleClaims()
    {
        var user = CreateDispatcher();

        var token = _sut.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be(_settings.Issuer);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == nameof(UserRole.Dispatcher));
    }

    [Fact]
    public void GenerateAccessToken_WithWarehouse_IncludesWarehouseIdClaim()
    {
        var user = CreateDispatcher();

        var token = _sut.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "warehouse_id");
    }

    [Fact]
    public void GenerateAccessToken_WithoutWarehouse_NoWarehouseIdClaim()
    {
        var phone = PhoneNumber.Create("+12025550123").Value!;
        var user = User.Create("mgr@example.com", "Test", "User", "hashed", phone, UserRole.Manager, null, null).Value!;

        var token = _sut.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().NotContain(c => c.Type == "warehouse_id");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentRawTokensEachCall()
    {
        var (raw1, _) = _sut.GenerateRefreshToken();
        var (raw2, _) = _sut.GenerateRefreshToken();

        raw1.Should().NotBe(raw2);
    }

    [Fact]
    public void GenerateRefreshToken_HashMatchesRawToken()
    {
        var (raw, hash) = _sut.GenerateRefreshToken();

        _sut.HashToken(raw).Should().Be(hash);
    }

    [Fact]
    public void HashToken_SameInput_ReturnsSameHash()
    {
        _sut.HashToken("my-token").Should().Be(_sut.HashToken("my-token"));
    }

    [Fact]
    public void HashToken_DifferentInput_ReturnsDifferentHash()
    {
        _sut.HashToken("token-a").Should().NotBe(_sut.HashToken("token-b"));
    }

    private static User CreateDispatcher()
    {
        var phone = PhoneNumber.Create("+12025550123").Value!;
        return User.Create("dispatcher@example.com", "Test", "User", "hashed", phone,
            UserRole.Dispatcher, Guid.NewGuid(), null).Value!;
    }
}
