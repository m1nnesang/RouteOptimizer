using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Application.Abstractions;

public interface IRouteRepository
{
    Task AddAsync(Route route, CancellationToken ct);
    Task<Route?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Route>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct);
}