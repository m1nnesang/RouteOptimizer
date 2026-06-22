using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct);
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct);
}
