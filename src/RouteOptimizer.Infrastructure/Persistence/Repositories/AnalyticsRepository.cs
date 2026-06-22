using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _db;

    public AnalyticsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(Guid? warehouseId, DateOnly date, CancellationToken ct)
    {
        var routeQuery = _db.Routes.AsNoTracking().Where(r => r.Date == date);

        if (warehouseId.HasValue)
            routeQuery = routeQuery.Where(r => r.WarehouseId == warehouseId.Value);

        var routeIds = await routeQuery.Select(r => r.Id).ToListAsync(ct);

        var stops = await BuildStopBreakdownAsync(routeIds, ct);
        var failures = await BuildFailureBreakdownAsync(routeIds, ct);
        var drivers = await BuildDriverPerformanceAsync(routeIds, ct);

        return new DashboardSnapshot(stops, failures, drivers);
    }

    private async Task<StopStatusBreakdown> BuildStopBreakdownAsync(IReadOnlyList<Guid> routeIds, CancellationToken ct)
    {
        var counts = await _db.Stops.AsNoTracking()
            .Where(s => routeIds.Contains(s.RouteId))
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int CountOf(StopStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        return new StopStatusBreakdown(
            counts.Sum(c => c.Count),
            CountOf(StopStatus.Pending),
            CountOf(StopStatus.InProgress),
            CountOf(StopStatus.Completed),
            CountOf(StopStatus.PartiallyCompleted),
            CountOf(StopStatus.Skipped),
            CountOf(StopStatus.Failed));
    }

    private async Task<IReadOnlyList<FailureReasonCount>> BuildFailureBreakdownAsync(
        IReadOnlyList<Guid> routeIds, CancellationToken ct)
    {
        var counts = await _db.DeliveryAttempts.AsNoTracking()
            .Where(a => routeIds.Contains(a.RouteId) && a.Outcome == DeliveryOutcome.Failed)
            .GroupBy(a => a.FailureReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts
            .Select(c => new FailureReasonCount(c.Reason?.ToString() ?? "Unknown", c.Count))
            .OrderByDescending(c => c.Count)
            .ToList();
    }

    private async Task<IReadOnlyList<DriverStopCounts>> BuildDriverPerformanceAsync(
        IReadOnlyList<Guid> routeIds, CancellationToken ct)
    {
        var counts = await _db.DeliveryAttempts.AsNoTracking()
            .Where(a => routeIds.Contains(a.RouteId))
            .GroupBy(a => a.DriverId)
            .Select(g => new DriverStopCounts(
                g.Key,
                g.Count(x => x.Outcome == DeliveryOutcome.Delivered),
                g.Count(x => x.Outcome == DeliveryOutcome.Failed)))
            .ToListAsync(ct);

        return counts;
    }
}
