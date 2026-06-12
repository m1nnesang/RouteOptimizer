using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    IReadOnlyList<IDomainEvent> GetPendingDomainEvents();
    void ClearAllDomainEvents();
}
