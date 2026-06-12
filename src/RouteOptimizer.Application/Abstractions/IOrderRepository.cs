using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllAsync(OrderStatus? status, DateOnly? date, int skip, int take, CancellationToken ct);

}
