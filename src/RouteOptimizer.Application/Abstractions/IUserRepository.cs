using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllDriversAsync(int skip, int take, CancellationToken ct);
}
