using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.VIewModels;

public partial class RoutesViewModel : ObservableObject
{
    private const string AllFilter = "All";

    private static readonly string[] RouteStatuses =
    [
        "Draft", "Optimized", "Assigned", "InProgress",
        "Completed", "Interrupted", "Cancelled"
    ];

    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;
    private Guid? _previousSelectedRouteId;

    public RoutesViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService)
    {
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        StatusFilters = new ObservableCollection<string>(
            new[] { AllFilter }.Concat(RouteStatuses));
        _ = LoadRoutesAsync();
    }

    [ObservableProperty]
    public partial ObservableCollection<RouteListItem> Routes { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRoute))]
    [NotifyPropertyChangedFor(nameof(NoRouteSelected))]
    public partial RouteListItem? SelectedRoute { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<RouteStop> SelectedRouteStops { get; set; } = [];

    [ObservableProperty]
    public partial double DepotLat { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double DepotLng { get; set; } = double.NaN;

    private List<WarehouseListItem>? _warehouses;

    [ObservableProperty]
    public partial ObservableCollection<string> StatusFilters { get; set; } = [];

    [ObservableProperty]
    public partial string SelectedStatusFilter { get; set; } = AllFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDateFilter))]
    public partial DateTime? SelectedDate { get; set; }

    public bool HasDateFilter => SelectedDate is not null;

    [ObservableProperty]
    public partial ObservableCollection<AlgorithmComparison> OptimizationComparisons { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOptimizationResult))]
    public partial bool HasOptimizationResultFlag { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasSelectedRoute => SelectedRoute is not null;

    public bool NoRouteSelected => SelectedRoute is null;

    public bool HasOptimizationResult => HasOptimizationResultFlag;

    partial void OnSelectedRouteChanged(RouteListItem? value)
    {
        if (value?.Id != _previousSelectedRouteId)
        {
            OptimizationComparisons = [];
            HasOptimizationResultFlag = false;
        }
        _previousSelectedRouteId = value?.Id;
        SelectedRouteStops = [];
        if (value is not null)
            _ = LoadRouteDetailAsync(value.Id);
    }

    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadRoutesAsync();

    partial void OnSelectedDateChanged(DateTime? value) => _ = LoadRoutesAsync();

    [RelayCommand]
    private void ClearDateFilter() => SelectedDate = null;

    [RelayCommand]
    private async Task LoadRoutesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        var previousId = SelectedRoute?.Id;
        try
        {
            var url = "api/routes?pageSize=100";
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != AllFilter)
                url += $"&status={SelectedStatusFilter}";
            if (SelectedDate is not null)
                url += $"&date={SelectedDate.Value:yyyy-MM-dd}";

            var result = await _apiHttpClient.GetAsync<PagedResult<RouteListItem>>(url);
            var items = (IEnumerable<RouteListItem>)(result?.Items ?? []);

            if (SelectedStatusFilter == AllFilter)
                items = items.Where(r => r.Status != "Interrupted");

            Routes = new ObservableCollection<RouteListItem>(items);

            if (previousId is { } id)
                SelectedRoute = Routes.FirstOrDefault(r => r.Id == id);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load routes. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRouteDetailAsync(Guid routeId)
    {
        try
        {
            var detail = await _apiHttpClient.GetAsync<RouteDetail>($"api/routes/{routeId}");
            var stops = (detail?.Stops ?? []).OrderBy(s => s.Sequence);
            SelectedRouteStops = new ObservableCollection<RouteStop>(stops);
            await SetDepotAsync(detail?.WarehouseId);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load route details.";
        }
    }

    private async Task SetDepotAsync(Guid? warehouseId)
    {
        if (warehouseId is null)
        {
            DepotLat = double.NaN;
            DepotLng = double.NaN;
            return;
        }

        _warehouses ??= await _apiHttpClient.GetAsync<List<WarehouseListItem>>("api/warehouses");
        var warehouse = _warehouses?.FirstOrDefault(w => w.Id == warehouseId.Value);

        DepotLat = warehouse?.Latitude ?? double.NaN;
        DepotLng = warehouse?.Longitude ?? double.NaN;
    }

    [RelayCommand]
    private async Task CreateRouteAsync()
    {
        var dialogViewModel = new CreateRouteDialogViewModel(_apiHttpClient);
        if (_dialogService.ShowCreateRouteDialog(dialogViewModel) != true || dialogViewModel.SelectedOrderIds.Count == 0)
            return;

        try
        {
            await _apiHttpClient.PostAsync<CreateRouteRequest, Guid>(
                "api/routes", new CreateRouteRequest(dialogViewModel.SelectedOrderIds, dialogViewModel.RouteDate));
            await LoadRoutesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to create route.";
        }
    }

    [RelayCommand]
    private async Task OptimizeAsync()
    {
        if (SelectedRoute is null)
            return;

        try
        {
            var routeDate = SelectedRoute.Date ?? DateOnly.FromDateTime(DateTime.Today);
            var result = await _apiHttpClient.PostAsync<OptimizeRouteRequest, OptimizeResult>(
                $"api/routes/{SelectedRoute.Id}/optimize", new OptimizeRouteRequest(routeDate));

            OptimizationComparisons = new ObservableCollection<AlgorithmComparison>(result?.Comparisons ?? []);
            HasOptimizationResultFlag = OptimizationComparisons.Count > 0;

            await LoadRouteDetailAsync(SelectedRoute.Id);
            await LoadRoutesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to optimize route.";
        }
    }

    [RelayCommand]
    private async Task AssignShiftAsync()
    {
        if (SelectedRoute is null)
            return;

        var dialogViewModel = new AssignShiftDialogViewModel(_apiHttpClient);
        if (_dialogService.ShowAssignShiftDialog(dialogViewModel) != true || dialogViewModel.SelectedShift is null)
            return;

        try
        {
            await _apiHttpClient.PostAsync(
                $"api/routes/{SelectedRoute.Id}/assign",
                new AssignRouteRequest(dialogViewModel.SelectedShift.Id));
            await LoadRoutesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to assign route to shift.";
        }
    }

    [RelayCommand]
    private async Task InsertUrgentOrderAsync()
    {
        if (SelectedRoute is null)
            return;

        var dialogViewModel = new InsertUrgentOrderDialogViewModel(_apiHttpClient);
        if (_dialogService.ShowInsertUrgentOrderDialog(dialogViewModel) != true || dialogViewModel.SelectedOrder is null)
            return;

        try
        {
            await _apiHttpClient.PostAsync(
                $"api/routes/{SelectedRoute.Id}/urgent-order",
                new InsertUrgentOrderRequest(dialogViewModel.SelectedOrder.Id));
            await LoadRouteDetailAsync(SelectedRoute.Id);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to insert urgent order.";
        }
    }

    [RelayCommand]
    private async Task HandoverAsync()
    {
        if (SelectedRoute is null)
            return;

        var pendingStops = SelectedRouteStops
            .Where(s => s.Status is "Pending" or "InProgress")
            .ToList();

        var dialogViewModel = new HandoverDialogViewModel(_apiHttpClient, pendingStops);
        if (_dialogService.ShowHandoverDialog(dialogViewModel) != true || !dialogViewModel.CanConfirm)
            return;

        try
        {
            await _apiHttpClient.PostAsync(
                $"api/routes/{SelectedRoute.Id}/handover",
                dialogViewModel.BuildRequest());
            await LoadRoutesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to hand over route.";
        }
    }
}
