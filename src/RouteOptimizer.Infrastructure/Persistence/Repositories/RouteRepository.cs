using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities.Route;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly AppDbContext _db;

    public RouteRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Route route, CancellationToken ct)
    {
        await _db.Routes.AddAsync(route, ct);
    }

    public async Task<Route?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<Route>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct)
    {
        return await _db.Routes.Where(x => x.WarehouseId == warehouseId).ToListAsync(ct);
    }
}