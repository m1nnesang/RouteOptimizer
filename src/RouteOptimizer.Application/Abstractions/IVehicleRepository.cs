using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Abstractions;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Vehicle>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct);
}