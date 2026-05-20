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
}