using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class DeliveryAttemptRepository : IDeliveryAttemptRepository
{
    private readonly AppDbContext _db;

    public DeliveryAttemptRepository(AppDbContext db)
    {
        _db = db;
    }
    public async Task AddAsync(DeliveryAttempt attempt, CancellationToken ct)
    {
        await _db.DeliveryAttempts.AddAsync(attempt, ct);
    }

    public async Task<IReadOnlyList<DeliveryAttempt>> GetByOrderIdAsync(Guid orderId, CancellationToken ct) =>
        await _db.DeliveryAttempts
            .AsNoTracking()
            .Where(a => a.OrderId == orderId)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync(ct);

    public async Task<DeliveryAttempt?> GetByPhotoKeyAsync(string photoKey, CancellationToken ct) =>
        await _db.DeliveryAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PhotoKey == photoKey, ct);
}
