using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IDeliveryAttemptRepository
{
    Task AddAsync(DeliveryAttempt attempt, CancellationToken ct);
}
