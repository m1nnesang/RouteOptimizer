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
}
