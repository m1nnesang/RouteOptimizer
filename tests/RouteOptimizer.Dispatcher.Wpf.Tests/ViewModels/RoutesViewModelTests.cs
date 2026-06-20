using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class RoutesViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();
    private readonly Mock<IDialogService> _dialog = new();

    private RoutesViewModel CreateViewModel()
    {
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        return new RoutesViewModel(_api.Object, _dialog.Object);
    }

    [Fact]
    public void Constructor_StatusFiltersContainsAllPlusSevenStatuses()
    {
        var vm = CreateViewModel();
        vm.StatusFilters.Should().HaveCount(8);
        vm.StatusFilters.First().Should().Be("All");
        vm.StatusFilters.Should().Contain("Interrupted");
    }

    [Fact]
    public void Constructor_DefaultFilterIsAll()
    {
        var vm = CreateViewModel();
        vm.SelectedStatusFilter.Should().Be("All");
    }

    [Fact]
    public async Task LoadRoutes_AllFilter_ExcludesInterrupted()
    {
        var routes = new List<RouteListItem>
        {
            new() { Id = Guid.NewGuid(), Status = "Draft" },
            new() { Id = Guid.NewGuid(), Status = "Interrupted" },
            new() { Id = Guid.NewGuid(), Status = "Completed" }
        };
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem> { Items = routes });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);

        vm.Routes.Should().HaveCount(2);
        vm.Routes.Should().NotContain(r => r.Status == "Interrupted");
    }

    [Fact]
    public async Task LoadRoutes_SpecificStatusFilter_DoesNotExcludeInterrupted()
    {
        var routes = new List<RouteListItem>
        {
            new() { Id = Guid.NewGuid(), Status = "Interrupted" }
        };
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem> { Items = routes });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedStatusFilter = "Interrupted";
        await Task.Yield();

        vm.Routes.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRoutes_SpecificStatus_UrlContainsStatus()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<RouteListItem>());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedStatusFilter = "Draft";
        await Task.Yield();

        capturedUrl.Should().Contain("status=Draft");
    }

    [Fact]
    public async Task LoadRoutes_WithDate_UrlContainsDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<RouteListItem>());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedDate = new DateTime(2025, 6, 1);
        await Task.Yield();

        capturedUrl.Should().Contain("date=2025-06-01");
    }

    [Fact]
    public async Task LoadRoutes_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);

        vm.ErrorMessage.Should().Be("Failed to load routes. Please check your connection.");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void SelectedRoute_SetToNull_ClearsDetailAndFlags()
    {
        var vm = CreateViewModel();
        vm.SelectedRoute = null;

        vm.OptimizationComparisons.Should().BeEmpty();
        vm.HasOptimizationResultFlag.Should().BeFalse();
        vm.SelectedRouteStops.Should().BeEmpty();
        vm.HasSelectedRoute.Should().BeFalse();
        vm.NoRouteSelected.Should().BeTrue();
    }

    [Fact]
    public async Task SelectedRoute_SetToValue_LoadsDetail()
    {
        var routeId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>($"api/routes/{routeId}", default))
            .ReturnsAsync(new RouteDetail
            {
                WarehouseId = warehouseId,
                Stops =
                [
                    new RouteStop { Id = Guid.NewGuid(), Sequence = 2 },
                    new RouteStop { Id = Guid.NewGuid(), Sequence = 0 },
                    new RouteStop { Id = Guid.NewGuid(), Sequence = 1 }
                ]
            });
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([new WarehouseListItem { Id = warehouseId, Latitude = 52.2, Longitude = 21.0 }]);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);

        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft" };
        await Task.Yield();

        vm.HasSelectedRoute.Should().BeTrue();
        vm.NoRouteSelected.Should().BeFalse();
        vm.SelectedRouteStops.Select(s => s.Sequence).Should().BeInAscendingOrder();
        vm.DepotLat.Should().Be(52.2);
        vm.DepotLng.Should().Be(21.0);
    }

    [Fact]
    public async Task SetDepot_WarehouseIdNull_SetsNaN()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>($"api/routes/{routeId}", default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);

        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft" };
        await Task.Yield();

        vm.DepotLat.Should().Be(double.NaN);
        vm.DepotLng.Should().Be(double.NaN);
    }

    [Fact]
    public async Task SetDepot_WarehouseNotFound_SetsNaN()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>($"api/routes/{routeId}", default))
            .ReturnsAsync(new RouteDetail { WarehouseId = Guid.NewGuid(), Stops = [] });
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([]);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);

        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft" };
        await Task.Yield();

        vm.DepotLat.Should().Be(double.NaN);
        vm.DepotLng.Should().Be(double.NaN);
    }

    [Fact]
    public void OptimizeAsync_NoSelectedRoute_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.OptimizeCommand.Execute(null);
        _api.Verify(a => a.PostAsync<OptimizeRouteRequest, OptimizeResult>(It.IsAny<string>(), It.IsAny<OptimizeRouteRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task OptimizeAsync_Success_PopulatesComparisons()
    {
        var routeId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>
            {
                Items = [new RouteListItem { Id = routeId, Status = "Assigned", Date = new DateOnly(2025, 6, 1) }]
            });
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { WarehouseId = warehouseId, Stops = [] });
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([new WarehouseListItem { Id = warehouseId }]);
        _api.Setup(a => a.PostAsync<OptimizeRouteRequest, OptimizeResult>($"api/routes/{routeId}/optimize", It.IsAny<OptimizeRouteRequest>(), default))
            .ReturnsAsync(new OptimizeResult
            {
                Comparisons = [new AlgorithmComparison { AlgorithmName = "2-opt", TotalDurationSeconds = 120 }]
            });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Assigned", Date = new DateOnly(2025, 6, 1) };
        await Task.Yield();

        await vm.OptimizeCommand.ExecuteAsync(null);

        vm.OptimizationComparisons.Should().HaveCount(1);
        vm.HasOptimizationResult.Should().BeTrue();
        vm.HasOptimizationResultFlag.Should().BeTrue();
    }

    [Fact]
    public async Task OptimizeAsync_EmptyComparisons_FlagFalse()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _api.Setup(a => a.PostAsync<OptimizeRouteRequest, OptimizeResult>(It.IsAny<string>(), It.IsAny<OptimizeRouteRequest>(), default))
            .ReturnsAsync(new OptimizeResult { Comparisons = [] });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft" };
        await Task.Yield();

        await vm.OptimizeCommand.ExecuteAsync(null);

        vm.HasOptimizationResultFlag.Should().BeFalse();
    }

    [Fact]
    public async Task OptimizeAsync_NullDate_UsesToday()
    {
        var routeId = Guid.NewGuid();
        OptimizeRouteRequest? captured = null;
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _api.Setup(a => a.PostAsync<OptimizeRouteRequest, OptimizeResult>(It.IsAny<string>(), It.IsAny<OptimizeRouteRequest>(), default))
            .Callback<string, OptimizeRouteRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(new OptimizeResult());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft", Date = null };
        await Task.Yield();

        await vm.OptimizeCommand.ExecuteAsync(null);

        captured!.RouteDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task OptimizeAsync_HttpError_SetsErrorMessage()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.PostAsync<OptimizeRouteRequest, OptimizeResult>(It.IsAny<string>(), It.IsAny<OptimizeRouteRequest>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft" };
        await Task.Yield();

        await vm.OptimizeCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to optimize route.");
    }

    [Fact]
    public void AssignShift_NoSelectedRoute_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.AssignShiftCommand.Execute(null);
        _dialog.Verify(d => d.ShowAssignShiftDialog(It.IsAny<AssignShiftDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task AssignShift_DialogCancelled_SkipsPost()
    {
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _dialog.Setup(d => d.ShowAssignShiftDialog(It.IsAny<AssignShiftDialogViewModel>())).Returns(false);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = Guid.NewGuid(), Status = "Optimized" };
        await Task.Yield();

        await vm.AssignShiftCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync(It.IsAny<string>(), It.IsAny<AssignRouteRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task AssignShift_Success_PostsAndReloads()
    {
        var routeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _dialog.Setup(d => d.ShowAssignShiftDialog(It.IsAny<AssignShiftDialogViewModel>()))
            .Callback<AssignShiftDialogViewModel>(vm => vm.SelectedShift = new ShiftListItem { Id = shiftId })
            .Returns(true);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Optimized" };
        await Task.Yield();

        await vm.AssignShiftCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync($"api/routes/{routeId}/assign", It.Is<AssignRouteRequest>(r => r.ShiftId == shiftId), default), Times.Once);
    }

    [Fact]
    public async Task InsertUrgentOrder_NoSelectedRoute_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.InsertUrgentOrderCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowInsertUrgentOrderDialog(It.IsAny<InsertUrgentOrderDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task InsertUrgentOrder_Success_PostsAndReloadsDetail()
    {
        var routeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _dialog.Setup(d => d.ShowInsertUrgentOrderDialog(It.IsAny<InsertUrgentOrderDialogViewModel>()))
            .Callback<InsertUrgentOrderDialogViewModel>(vm => vm.SelectedOrder = new OrderListItem { Id = orderId })
            .Returns(true);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "InProgress" };
        await Task.Yield();

        await vm.InsertUrgentOrderCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync($"api/routes/{routeId}/urgent-order", It.Is<InsertUrgentOrderRequest>(r => r.OrderId == orderId), default), Times.Once);
    }

    [Fact]
    public async Task Handover_NoSelectedRoute_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.HandoverCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowHandoverDialog(It.IsAny<HandoverDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task Handover_OnlyPassesPendingAndInProgressStops()
    {
        var routeId = Guid.NewGuid();
        IEnumerable<RouteStop>? capturedStops = null;
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>($"api/routes/{routeId}", default))
            .ReturnsAsync(new RouteDetail
            {
                Stops =
                [
                    new RouteStop { Id = Guid.NewGuid(), Status = "Pending", Sequence = 0 },
                    new RouteStop { Id = Guid.NewGuid(), Status = "Completed", Sequence = 1 },
                    new RouteStop { Id = Guid.NewGuid(), Status = "InProgress", Sequence = 2 }
                ]
            });
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([]);
        _dialog.Setup(d => d.ShowHandoverDialog(It.IsAny<HandoverDialogViewModel>()))
            .Callback<HandoverDialogViewModel>(vm =>
            {
                capturedStops = vm.PendingStops.Select(s => new RouteStop { Status = "Pending" });
                vm.IsReturnToPool = true;
            })
            .Returns(true);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "InProgress" };
        await Task.Yield();

        await vm.HandoverCommand.ExecuteAsync(null);

        _dialog.Verify(d => d.ShowHandoverDialog(It.Is<HandoverDialogViewModel>(hvm => hvm.PendingStops.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task DriverLocation_MatchingRoute_UpdatesDriverPosition()
    {
        var routeId = Guid.NewGuid();
        var hub = new Mock<IRouteHubService>();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object, hub.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "InProgress" };
        await Task.Yield();

        hub.Raise(h => h.DriverLocation += null,
            new DriverLocationEvent { RouteId = routeId, Latitude = 52.1, Longitude = 21.0 });

        vm.DriverLat.Should().Be(52.1);
        vm.DriverLng.Should().Be(21.0);
    }

    [Fact]
    public async Task DriverLocation_OtherRoute_IsIgnored()
    {
        var routeId = Guid.NewGuid();
        var hub = new Mock<IRouteHubService>();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object, hub.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "InProgress" };
        await Task.Yield();

        hub.Raise(h => h.DriverLocation += null,
            new DriverLocationEvent { RouteId = Guid.NewGuid(), Latitude = 52.1, Longitude = 21.0 });

        vm.DriverLat.Should().Be(double.NaN);
        vm.DriverLng.Should().Be(double.NaN);
    }

    [Fact]
    public async Task RouteChanged_ReloadsRoutes()
    {
        var hub = new Mock<IRouteHubService>();
        var callCount = 0;
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .Callback(() => callCount++)
            .ReturnsAsync(new PagedResult<RouteListItem>());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object, hub.Object);
        await Task.Yield();
        var before = callCount;

        hub.Raise(h => h.RouteChanged += null, new RouteChangedEvent { RouteId = Guid.NewGuid() });

        callCount.Should().BeGreaterThan(before);
    }

    [Fact]
    public async Task SelectedRoute_Changed_ResetsDriverPosition()
    {
        var hub = new Mock<IRouteHubService>();
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        var vm = new RoutesViewModel(_api.Object, _dialog.Object, hub.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "InProgress" };
        await Task.Yield();
        hub.Raise(h => h.DriverLocation += null,
            new DriverLocationEvent { RouteId = routeId, Latitude = 52.1, Longitude = 21.0 });

        vm.SelectedRoute = new RouteListItem { Id = Guid.NewGuid(), Status = "InProgress" };

        vm.DriverLat.Should().Be(double.NaN);
        vm.DriverLng.Should().Be(double.NaN);
    }

    [Fact]
    public async Task CancelRoute_ConfirmDeclined_DoesNotPost()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Draft" };
        await Task.Yield();

        await vm.CancelRouteCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CancelRoute_Confirmed_PostsCancel()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "Optimized" };
        await Task.Yield();

        await vm.CancelRouteCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync($"api/routes/{routeId}/cancel", default), Times.Once);
    }

    [Fact]
    public async Task InterruptRoute_Confirmed_PostsInterrupt()
    {
        var routeId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        _api.Setup(a => a.GetAsync<RouteDetail>(It.IsAny<string>(), default))
            .ReturnsAsync(new RouteDetail { Stops = [] });
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        vm.SelectedRoute = new RouteListItem { Id = routeId, Status = "InProgress" };
        await Task.Yield();

        await vm.InterruptRouteCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync($"api/routes/{routeId}/interrupt", default), Times.Once);
    }

    [Fact]
    public void CanCancelAndInterrupt_DependOnStatus()
    {
        var vm = CreateViewModel();

        vm.SelectedRoute = new RouteListItem { Id = Guid.NewGuid(), Status = "Draft" };
        vm.CanCancel.Should().BeTrue();
        vm.CanInterrupt.Should().BeFalse();

        vm.SelectedRoute = new RouteListItem { Id = Guid.NewGuid(), Status = "InProgress" };
        vm.CanCancel.Should().BeFalse();
        vm.CanInterrupt.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmpty_TrueWhenNoRoutesAndNotLoading()
    {
        _api.Setup(a => a.GetAsync<PagedResult<RouteListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<RouteListItem>());
        var vm = new RoutesViewModel(_api.Object, _dialog.Object);
        await Task.Yield();

        vm.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ClearDateFilter_ResetsDate()
    {
        var vm = CreateViewModel();
        vm.SelectedDate = DateTime.Today;
        vm.ClearDateFilterCommand.Execute(null);
        vm.SelectedDate.Should().BeNull();
        vm.HasDateFilter.Should().BeFalse();
    }
}
