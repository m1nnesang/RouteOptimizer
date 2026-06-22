using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Analytics.GetDashboard;

public sealed record DashboardDto(
    DateOnly Date,
    StopStatusBreakdown Stops,
    IReadOnlyList<FailureReasonCount> FailureBreakdown,
    IReadOnlyList<DriverPerformanceDto> DriverPerformance);

public sealed record DriverPerformanceDto(Guid DriverId, string DriverName, int Delivered, int Failed);
