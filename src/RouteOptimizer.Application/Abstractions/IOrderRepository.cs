using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
}
