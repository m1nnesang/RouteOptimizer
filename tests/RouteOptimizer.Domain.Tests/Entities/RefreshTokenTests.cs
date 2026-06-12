using FluentAssertions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidArgs_ReturnsTokenWithExpectedProperties()
    {
        var userId = Guid.NewGuid();
        var tokenHash = "abc123hash";
        var lifetime = TimeSpan.FromDays(7);

        var before = DateTime.UtcNow;
        var token = RefreshToken.Create(userId, tokenHash, lifetime);
        var after = DateTime.UtcNow;

        token.Should().NotBeNull();
        token.Id.Should().NotBe(Guid.Empty);
        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be(tokenHash);
        token.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        token.ExpiresAt.Should().BeCloseTo(before.Add(lifetime), TimeSpan.FromSeconds(2));
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TwoCalls_ReturnDifferentIds()
    {
        var userId = Guid.NewGuid();

        var t1 = RefreshToken.Create(userId, "hash1", TimeSpan.FromDays(1));
        var t2 = RefreshToken.Create(userId, "hash2", TimeSpan.FromDays(1));

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void IsExpired_TokenWithFutureExpiry_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_TokenWithPastExpiry_ReturnsTrue()
    {

        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromSeconds(-1));

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_NewToken_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_AfterRevoke_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsActive_FreshToken_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ExpiredToken_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromSeconds(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_RevokedToken_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_ActiveToken_SetsRevokedAt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));
        var before = DateTime.UtcNow;

        token.Revoke();

        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Revoke_AlreadyRevokedToken_ThrowsInvalidOperationException()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));
        token.Revoke();

        var act = () => token.Revoke();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been revoked*");
    }
}
