using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class AssignShiftDialogViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    [Fact]
    public async Task LoadShifts_WithoutDate_UsesBaseUrl()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<ShiftListItem>());
        var vm = new AssignShiftDialogViewModel(_api.Object);

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        capturedUrl.Should().Be("api/drivers/shifts?pageSize=200");
    }

    [Fact]
    public async Task LoadShifts_WithDate_UrlContainsDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<ShiftListItem>());
        var vm = new AssignShiftDialogViewModel(_api.Object);
        vm.FilterDate = new DateTime(2025, 6, 10);

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        capturedUrl.Should().Contain("date=2025-06-10");
    }

    [Fact]
    public async Task LoadShifts_PopulatesShifts()
    {
        var shifts = new List<ShiftListItem>
        {
            new() { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow }
        };
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<ShiftListItem> { Items = shifts });
        var vm = new AssignShiftDialogViewModel(_api.Object);

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        vm.Shifts.Should().HaveCount(2);
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadShifts_ShowsOnlyActiveShifts()
    {
        var shifts = new List<ShiftListItem>
        {
            new() { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow }
        };
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<ShiftListItem> { Items = shifts });
        var vm = new AssignShiftDialogViewModel(_api.Object);

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        vm.Shifts.Should().ContainSingle();
        vm.Shifts[0].ShiftStatus.Should().Be("Active");
    }

    [Fact]
    public async Task LoadShifts_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new AssignShiftDialogViewModel(_api.Object);

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to load shifts. Please check your connection.");
        vm.HasError.Should().BeTrue();
    }

    [Fact]
    public void HasSelectedShift_WhenShiftSelected_True()
    {
        var vm = new AssignShiftDialogViewModel(_api.Object);
        vm.SelectedShift = new ShiftListItem { Id = Guid.NewGuid() };
        vm.HasSelectedShift.Should().BeTrue();
    }

    [Fact]
    public void HasSelectedShift_WhenNull_False()
    {
        var vm = new AssignShiftDialogViewModel(_api.Object);
        vm.HasSelectedShift.Should().BeFalse();
    }
}
