using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class DriverShiftRepository : IDriverShiftRepository
{
    private readonly AppDbContext _db;

    public DriverShiftRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(DriverShift shift, CancellationToken ct)
    {
        await _db.DriverShifts.AddAsync(shift, ct);
    }

    public async Task<DriverShift?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.DriverShifts.FindAsync(new object[] { id }, ct);
    }

    public async Task<DriverShift?> GetActiveShiftByDriverIdAsync(Guid driverId, CancellationToken ct)
    {
        return await _db.DriverShifts
            .Where(x => x.DriverId == driverId && x.StartedAt != null && x.EndedAt == null)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<DriverShift>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        return await _db.DriverShifts
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<DriverShift> Items, int TotalCount)> GetAllAsync(
        Guid? warehouseId, DateOnly? date, int skip, int take, CancellationToken ct)
    {
        var query = _db.DriverShifts.AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (date.HasValue)
            query = query.Where(x => x.ShiftDate == date.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.ShiftDate).Skip(skip).Take(take).ToListAsync(ct);

        return (items, totalCount);
    }
}