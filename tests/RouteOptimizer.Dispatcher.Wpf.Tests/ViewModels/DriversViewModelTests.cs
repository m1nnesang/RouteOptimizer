using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class DriversViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    private DriversViewModel CreateViewModel(List<ShiftListItem>? shifts = null)
    {
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<ShiftListItem> { Items = shifts ?? [] });
        return new DriversViewModel(_api.Object);
    }

    [Fact]
    public void Constructor_LoadsShifts()
    {
        var shifts = new List<ShiftListItem> { new() { Id = Guid.NewGuid() } };
        var vm = CreateViewModel(shifts);
        vm.Shifts.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadShifts_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new DriversViewModel(_api.Object);

        vm.ErrorMessage.Should().Be("Failed to load shifts. Please check your connection.");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadShifts_WithDate_UrlContainsDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<ShiftListItem>());
        var vm = new DriversViewModel(_api.Object);
        vm.FilterDate = new DateTime(2025, 6, 15);
        await Task.Yield();

        capturedUrl.Should().Contain("date=2025-06-15");
    }

    [Fact]
    public async Task LoadShifts_WithoutDate_UrlHasNoDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<ShiftListItem>());
        var vm = new DriversViewModel(_api.Object);
        await Task.Yield();

        capturedUrl.Should().NotContain("date=");
    }

    [Fact]
    public void FilterDate_Changed_TriggersReload()
    {
        var vm = CreateViewModel();
        vm.FilterDate = DateTime.Today;
        _api.Verify(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default), Times.AtLeast(2));
    }

    [Fact]
    public void ClearDateFilter_ResetsDate()
    {
        var vm = CreateViewModel();
        vm.FilterDate = DateTime.Today;
        vm.ClearDateFilterCommand.Execute(null);
        vm.FilterDate.Should().BeNull();
    }

    [Fact]
    public void LoadShifts_NullResult_ShiftsEmpty()
    {
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync((PagedResult<ShiftListItem>?)null);
        var vm = new DriversViewModel(_api.Object);
        vm.Shifts.Should().BeEmpty();
        vm.HasError.Should().BeFalse();
    }
}
