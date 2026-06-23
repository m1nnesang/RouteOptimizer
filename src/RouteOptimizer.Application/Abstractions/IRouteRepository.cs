using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Abstractions;

public interface IRouteRepository
{
    Task AddAsync(Route route, CancellationToken ct);
    Task AddStopAsync(Stop stop, CancellationToken ct);
    Task<Route?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Route>> GetByAssignedShiftIdAsync(Guid shiftId, CancellationToken ct);
    Task<IReadOnlyList<Route>> GetByDriverIdAndDateAsync(Guid driverId, DateOnly date, CancellationToken ct);
    Task<(IReadOnlyList<Route> Items, int TotalCount)> GetAllAsync(Guid? warehouseId, RouteStatus? status, DateOnly? date, int skip, int take, CancellationToken ct);
}
