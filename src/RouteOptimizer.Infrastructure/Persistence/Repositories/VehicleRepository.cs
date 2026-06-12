using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _db;

    public VehicleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct)
    {
        await _db.Vehicles.AddAsync(vehicle, ct);
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct)
    {
        _db.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Vehicle vehicle, CancellationToken ct)
    {
        _db.Vehicles.Remove(vehicle);
        return Task.CompletedTask;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.FindAsync<Vehicle>(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<Vehicle>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct)
    {
        return await _db.Vehicles.Where(x => x.WarehouseId == warehouseId).ToListAsync(ct);
    }
}