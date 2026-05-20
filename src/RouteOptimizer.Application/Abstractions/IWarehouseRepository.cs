using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IWarehouseRepository
{
    Task AddAsync(Warehouse warehouse, CancellationToken ct);
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct);
}