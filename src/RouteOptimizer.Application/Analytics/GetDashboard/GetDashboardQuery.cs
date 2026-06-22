using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Analytics.GetDashboard;

public sealed record GetDashboardQuery(DateOnly Date) : IQuery<DashboardDto>;
