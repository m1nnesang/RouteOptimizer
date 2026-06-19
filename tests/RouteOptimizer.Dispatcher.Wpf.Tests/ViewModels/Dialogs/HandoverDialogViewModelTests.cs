using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class HandoverDialogViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    private static RouteStop MakeStop(string status = "Pending") =>
        new() { Id = Guid.NewGuid(), Status = status, City = "Warszawa", Street = "ul. A 1" };

    private HandoverDialogViewModel CreateViewModel(IEnumerable<RouteStop>? stops = null) =>
        new(_api.Object, stops ?? [MakeStop(), MakeStop("InProgress")]);

    [Fact]
    public void Constructor_DefaultIsTransferAll()
    {
        var vm = CreateViewModel();
        vm.IsTransferAll.Should().BeTrue();
        vm.IsDistribute.Should().BeFalse();
        vm.IsReturnToPool.Should().BeFalse();
    }

    [Fact]
    public void Constructor_PopulatesPendingStops()
    {
        var vm = CreateViewModel();
        vm.PendingStops.Should().HaveCount(2);
    }

    [Fact]
    public void CanConfirm_ReturnToPool_AlwaysTrue()
    {
        var vm = CreateViewModel();
        vm.IsTransferAll = false;
        vm.IsReturnToPool = true;
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void CanConfirm_TransferAll_RequiresTargetShift()
    {
        var vm = CreateViewModel();
        vm.IsTransferAll = true;
        vm.TargetShift = null;
        vm.CanConfirm.Should().BeFalse();

        vm.TargetShift = new ShiftListItem { Id = Guid.NewGuid() };
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void CanConfirm_Distribute_RequiresAllStopsAssigned()
    {
        var vm = CreateViewModel();
        vm.IsTransferAll = false;
        vm.IsDistribute = true;

        vm.CanConfirm.Should().BeFalse();

        var shift = new ShiftListItem { Id = Guid.NewGuid() };
        foreach (var stop in vm.PendingStops)
            stop.SelectedShift = shift;

        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void CanConfirm_Distribute_EmptyStops_False()
    {
        var vm = new HandoverDialogViewModel(_api.Object, []);
        vm.IsTransferAll = false;
        vm.IsDistribute = true;
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void StopShiftChange_RecalculatesCanConfirm()
    {
        var vm = CreateViewModel([MakeStop()]);
        vm.IsTransferAll = false;
        vm.IsDistribute = true;

        var beforeChange = vm.CanConfirm;
        vm.PendingStops[0].SelectedShift = new ShiftListItem { Id = Guid.NewGuid() };
        var afterChange = vm.CanConfirm;

        beforeChange.Should().BeFalse();
        afterChange.Should().BeTrue();
    }

    [Fact]
    public void BuildRequest_ReturnToPool_CorrectType()
    {
        var vm = CreateViewModel();
        vm.IsTransferAll = false;
        vm.IsReturnToPool = true;

        var req = vm.BuildRequest();

        req.Type.Should().Be(HandoverType.ReturnToPool);
        req.TargetShiftId.Should().BeNull();
        req.Assignments.Should().BeNull();
    }

    [Fact]
    public void BuildRequest_TransferAll_IncludesTargetShiftId()
    {
        var shiftId = Guid.NewGuid();
        var vm = CreateViewModel();
        vm.IsTransferAll = true;
        vm.TargetShift = new ShiftListItem { Id = shiftId };

        var req = vm.BuildRequest();

        req.Type.Should().Be(HandoverType.TransferAll);
        req.TargetShiftId.Should().Be(shiftId);
        req.Assignments.Should().BeNull();
    }

    [Fact]
    public void BuildRequest_Distribute_GroupsStopsByShift()
    {
        var shift1 = new ShiftListItem { Id = Guid.NewGuid() };
        var shift2 = new ShiftListItem { Id = Guid.NewGuid() };
        var stops = new[]
        {
            MakeStop(), MakeStop(), MakeStop()
        };
        var vm = new HandoverDialogViewModel(_api.Object, stops);
        vm.IsTransferAll = false;
        vm.IsDistribute = true;
        vm.PendingStops[0].SelectedShift = shift1;
        vm.PendingStops[1].SelectedShift = shift1;
        vm.PendingStops[2].SelectedShift = shift2;

        var req = vm.BuildRequest();

        req.Type.Should().Be(HandoverType.Distribute);
        req.Assignments.Should().HaveCount(2);
        req.Assignments!.First(a => a.ShiftId == shift1.Id).StopIds.Should().HaveCount(2);
        req.Assignments!.First(a => a.ShiftId == shift2.Id).StopIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadShifts_PopulatesAvailableShifts()
    {
        var shifts = new List<ShiftListItem> { new() { Id = Guid.NewGuid() } };
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>("api/drivers/shifts?pageSize=200", default))
            .ReturnsAsync(new PagedResult<ShiftListItem> { Items = shifts });
        var vm = CreateViewModel();

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        vm.AvailableShifts.Should().HaveCount(1);
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadShifts_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<ShiftListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = CreateViewModel();

        await vm.LoadShiftsCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to load shifts. Please check your connection.");
    }
}
