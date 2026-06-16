using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IDriverShiftRepository
{
    Task AddAsync(DriverShift shift, CancellationToken ct);
    Task<DriverShift?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<DriverShift?> GetActiveShiftByDriverIdAsync(Guid driverId, CancellationToken ct);
    Task<IReadOnlyList<DriverShift>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<(IReadOnlyList<DriverShift> Items, int TotalCount)> GetAllAsync(Guid? warehouseId, DateOnly? date, int skip, int take, CancellationToken ct);
}
