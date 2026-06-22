using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;

    public PasswordResetTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct)
    {
        await _db.PasswordResetTokens.AddAsync(token, ct);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return await _db.PasswordResetTokens.FirstOrDefaultAsync(x => x.Token == token, ct);
    }
}
