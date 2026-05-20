using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Entities;

public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; }
    public string TokenHash { get; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() : base(default)
    {
        UserId = Guid.Empty;
        TokenHash = null!;
        CreatedAt = default;
        ExpiresAt = default;
        RevokedAt = null;
    }


    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime createdAt, DateTime expiresAt) : base(id)
    {
        (UserId, TokenHash, CreatedAt, ExpiresAt) = (userId, tokenHash, createdAt, expiresAt);
    }

    public static RefreshToken Create(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken(Guid.NewGuid(), userId, tokenHash, now, now.Add(lifetime));
    }

    public void Revoke()
    {
        if (IsRevoked)
            throw new InvalidOperationException("Token has already been revoked");

        RevokedAt = DateTime.UtcNow;
    }
}
