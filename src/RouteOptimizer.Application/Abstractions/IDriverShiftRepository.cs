using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IDriverShiftRepository
{
    Task AddAsync(DriverShift shift, CancellationToken ct);
    Task<DriverShift?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<DriverShift?> GetActiveShiftByDriverIdAsync(Guid driverId, CancellationToken ct);
}
