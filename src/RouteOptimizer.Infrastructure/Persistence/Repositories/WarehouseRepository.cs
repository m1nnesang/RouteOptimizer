using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _db;

    public WarehouseRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct)
    {
        await _db.Warehouses.AddAsync(warehouse, ct);
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken ct)
    {
        _db.Warehouses.Update(warehouse);
    }

    public async Task DeleteAsync(Warehouse warehouse, CancellationToken ct)
    {
        _db.Warehouses.Remove(warehouse);
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Warehouses.FindAsync(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Warehouses.ToListAsync(ct);
    }
}
