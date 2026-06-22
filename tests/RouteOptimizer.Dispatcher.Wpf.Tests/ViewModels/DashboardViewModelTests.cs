using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    [Fact]
    public async Task Load_BuildsUrlWithSelectedDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<DashboardResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new DashboardResult());
        var vm = new DashboardViewModel(_api.Object) { SelectedDate = new DateTime(2026, 6, 22) };

        await vm.LoadCommand.ExecuteAsync(null);

        capturedUrl.Should().Be("api/analytics/dashboard?date=2026-06-22");
    }

    [Fact]
    public async Task Load_PopulatesStopsAndCollections()
    {
        var result = new DashboardResult
        {
            Stops = new StopStatusBreakdown { Total = 10, Completed = 7, Failed = 1 },
            FailureBreakdown = [new FailureReasonCount { Reason = "FirmClosed", Count = 1 }],
            DriverPerformance =
            [
                new DriverPerformance { DriverName = "Anna", Delivered = 7, Failed = 1 }
            ]
        };
        _api.Setup(a => a.GetAsync<DashboardResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        var vm = new DashboardViewModel(_api.Object);

        await vm.LoadCommand.ExecuteAsync(null);

        vm.Stops.Total.Should().Be(10);
        vm.FailureBreakdown.Should().ContainSingle(f => f.Reason == "FirmClosed");
        vm.DriverPerformance.Should().ContainSingle(d => d.DriverName == "Anna");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Load_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<DashboardResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));
        var vm = new DashboardViewModel(_api.Object);

        await vm.LoadCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().NotBeEmpty();
        vm.IsLoading.Should().BeFalse();
    }
}
