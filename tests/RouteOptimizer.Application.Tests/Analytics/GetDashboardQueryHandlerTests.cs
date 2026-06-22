using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Analytics.GetDashboard;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Analytics;

public class GetDashboardQueryHandlerTests
{
    private readonly Mock<IAnalyticsRepository> _analyticsRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetDashboardQueryHandler _handler;

    public GetDashboardQueryHandlerTests()
    {
        _handler = new GetDashboardQueryHandler(_analyticsRepo.Object, _userRepo.Object, _currentUser.Object);
    }

    private static User Driver(string first, string last) =>
        User.Create($"{first}@test.com", first, last, "hash",
            PhoneNumber.Create("+48601234567").Value!, UserRole.Driver, Guid.NewGuid(),
            new DriverProfile(LicenseCategory.B)).Value!;

    [Fact]
    public async Task Handle_PassesCurrentUserWarehouseScopeToRepository()
    {
        var warehouseId = Guid.NewGuid();
        var date = new DateOnly(2026, 6, 22);
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        _analyticsRepo.Setup(x => x.GetDashboardAsync(warehouseId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSnapshot(
                new StopStatusBreakdown(0, 0, 0, 0, 0, 0, 0), [], []));
        _userRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _handler.Handle(new GetDashboardQuery(date), default);

        _analyticsRepo.Verify(x => x.GetDashboardAsync(warehouseId, date, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResolvesDriverNamesAndOrdersByDelivered()
    {
        var anna = Driver("Anna", "Nowak");
        var piotr = Driver("Piotr", "Was");
        var date = new DateOnly(2026, 6, 22);
        _currentUser.Setup(x => x.WarehouseId).Returns((Guid?)null);
        _analyticsRepo.Setup(x => x.GetDashboardAsync(null, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSnapshot(
                new StopStatusBreakdown(10, 2, 1, 6, 0, 0, 1),
                [new FailureReasonCount("CustomerNotAtHome", 1)],
                [new DriverStopCounts(anna.Id, 3, 1), new DriverStopCounts(piotr.Id, 8, 0)]));
        _userRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([anna, piotr]);

        var result = await _handler.Handle(new GetDashboardQuery(date), default);

        result.Stops.Total.Should().Be(10);
        result.FailureBreakdown.Should().ContainSingle(f => f.Reason == "CustomerNotAtHome");
        result.DriverPerformance.Should().HaveCount(2);
        result.DriverPerformance[0].DriverId.Should().Be(piotr.Id);
        result.DriverPerformance[0].DriverName.Should().Be("Piotr Was");
        result.DriverPerformance[0].Delivered.Should().Be(8);
    }
}
