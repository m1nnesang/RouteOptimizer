using System.Security.Cryptography;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Entities;

public class UserInvitation : AggregateRoot<Guid>
{
    public Guid UserId { get; }
    public string Token { get; }
    
    public DateTime ExpiresAt  { get; }
    public DateTime? UsedAt { get; private set; }
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsUsed => UsedAt is not null;

    public void Use()
    {
        if (IsUsed)
            throw new InvalidOperationException("Invitation has already been used");
        
        if (IsExpired)
            throw new InvalidOperationException("Invitation has expired");
        
        UsedAt = DateTime.UtcNow;
    }
    
    private UserInvitation(Guid id, Guid userId, string token, DateTime expiresAt, DateTime? usedAt) : base(id) 
        => (UserId, Token, ExpiresAt, UsedAt) = (userId, token, expiresAt, usedAt);

    public static UserInvitation Create(Guid userId,TimeSpan expirationPeriod)
    {
        var expiresAt = DateTime.UtcNow.Add(expirationPeriod);
        var id = Guid.NewGuid();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        
        return new UserInvitation(id, userId, token, expiresAt, null);
    }
}