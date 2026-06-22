using MediatR;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Analytics.GetDashboard;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetDashboardQueryHandler(
        IAnalyticsRepository analyticsRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _analyticsRepository = analyticsRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var snapshot = await _analyticsRepository.GetDashboardAsync(_currentUser.WarehouseId, request.Date, ct);

        var driverIds = snapshot.DriverPerformance.Select(d => d.DriverId).Distinct().ToList();
        var drivers = await _userRepository.GetByIdsAsync(driverIds, ct);
        var driverNames = drivers.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var performance = snapshot.DriverPerformance
            .Select(d => new DriverPerformanceDto(
                d.DriverId,
                driverNames.GetValueOrDefault(d.DriverId, string.Empty),
                d.Delivered,
                d.Failed))
            .OrderByDescending(d => d.Delivered)
            .ToList();

        return new DashboardDto(request.Date, snapshot.Stops, snapshot.FailureBreakdown, performance);
    }
}
