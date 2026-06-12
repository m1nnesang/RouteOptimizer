using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Abstractions;

public interface IRouteRepository
{
    Task AddAsync(Route route, CancellationToken ct);
    Task<Route?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<(IReadOnlyList<Route> Items, int TotalCount)> GetAllAsync(Guid? warehouseId, RouteStatus? status, int skip, int take, CancellationToken ct);
}
