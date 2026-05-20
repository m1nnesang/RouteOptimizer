using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class UserInvitationRepository : IUserInvitationRepository
{
    private readonly AppDbContext _db;

    public UserInvitationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(UserInvitation invitation, CancellationToken ct)
    {
        await _db.UserInvitations.AddAsync(invitation, ct);
    }

    public async Task<UserInvitation?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return await _db.UserInvitations.FirstOrDefaultAsync(x => x.Token == token, ct);
    }
}