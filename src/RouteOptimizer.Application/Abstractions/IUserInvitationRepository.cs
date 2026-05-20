using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IUserInvitationRepository
{
    Task AddAsync(UserInvitation invitation, CancellationToken ct);
    Task<UserInvitation?> GetByTokenAsync(string token, CancellationToken ct);
}