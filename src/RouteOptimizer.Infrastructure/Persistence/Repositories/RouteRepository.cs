using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;

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
            .Include(r => r.Stops.OrderBy(s => s.Sequence))
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<Route>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct)
    {
        return await _db.Routes.Where(x => x.WarehouseId == warehouseId).ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Route> Items, int TotalCount)> GetAllAsync(Guid? warehouseId, RouteStatus? status,
        int skip, int take, CancellationToken ct)
    {
        var query = _db.Routes
            .Include(r => r.Stops.OrderBy(s => s.Sequence))
            .AsQueryable();

        if (warehouseId is not null) query = query.Where(x => x.WarehouseId == warehouseId);

        if (status is not null) query = query.Where(x => x.Status == status);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(r => r.Id).Skip(skip).Take(take).ToListAsync(ct);

        return (items, totalCount);
    }
}
