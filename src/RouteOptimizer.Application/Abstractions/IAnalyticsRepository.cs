namespace RouteOptimizer.Application.Abstractions;

public interface IAnalyticsRepository
{
    Task<DashboardSnapshot> GetDashboardAsync(Guid? warehouseId, DateOnly date, CancellationToken ct);
}

public sealed record DashboardSnapshot(
    StopStatusBreakdown Stops,
    IReadOnlyList<FailureReasonCount> FailureBreakdown,
    IReadOnlyList<DriverStopCounts> DriverPerformance);

public sealed record StopStatusBreakdown(
    int Total,
    int Pending,
    int InProgress,
    int Completed,
    int PartiallyCompleted,
    int Skipped,
    int Failed);

public sealed record FailureReasonCount(string Reason, int Count);

public sealed record DriverStopCounts(Guid DriverId, int Delivered, int Failed);
