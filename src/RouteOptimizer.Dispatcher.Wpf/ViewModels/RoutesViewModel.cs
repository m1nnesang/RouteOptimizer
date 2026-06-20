using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class RoutesViewModel : ObservableObject, IDisposable
{
    private const string AllFilter = "All";

    private static readonly string[] RouteStatuses =
    [
        "Draft", "Optimized", "Assigned", "InProgress",
        "Completed", "Interrupted", "Cancelled"
    ];

    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;
    private readonly IRouteHubService? _routeHub;
    private Guid? _previousSelectedRouteId;
    private List<RouteListItem> _allRoutes = [];

    public RoutesViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService,
        IRouteHubService? routeHub = null)
    {
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        _routeHub = routeHub;
        StatusFilters = new ObservableCollection<string>(
            new[] { AllFilter }.Concat(RouteStatuses));

        if (_routeHub is not null)
        {
            _routeHub.RouteStarted += OnRouteStarted;
            _routeHub.StopCompleted += OnStopChanged;
            _routeHub.StopFailed += OnStopChanged;
            _routeHub.StopSkipped += OnStopChanged;
            _routeHub.RouteChanged += OnRouteChanged;
            _routeHub.DriverLocation += OnDriverLocation;
        }

        _ = LoadRoutesAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
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

    [ObservableProperty]
    public partial double DriverLat { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double DriverLng { get; set; } = double.NaN;

    private List<WarehouseListItem>? _warehouses;

    [ObservableProperty]
    public partial ObservableCollection<string> StatusFilters { get; set; } = [];

    [ObservableProperty]
    public partial string SelectedStatusFilter { get; set; } = AllFilter;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

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
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsEmpty => !IsLoading && !HasError && Routes.Count == 0;

    public bool HasSelectedRoute => SelectedRoute is not null;

    public bool NoRouteSelected => SelectedRoute is null;

    public bool HasOptimizationResult => HasOptimizationResultFlag;

    public bool CanOptimize => SelectedRoute?.Status == "Draft";

    public bool CanAssignShift => SelectedRoute?.Status == "Optimized";

    public bool CanInsertUrgentOrder => SelectedRoute?.Status == "InProgress";

    public bool CanHandover => SelectedRoute?.Status == "InProgress";

    public bool CanCancel => SelectedRoute?.Status is "Draft" or "Optimized" or "Assigned";

    public bool CanInterrupt => SelectedRoute?.Status is "InProgress" or "Assigned";

    partial void OnSelectedRouteChanged(RouteListItem? value)
    {
        if (value?.Id != _previousSelectedRouteId)
        {
            OptimizationComparisons = [];
            HasOptimizationResultFlag = false;
        }
        _previousSelectedRouteId = value?.Id;
        SelectedRouteStops = [];
        DriverLat = double.NaN;
        DriverLng = double.NaN;
        OptimizeCommand.NotifyCanExecuteChanged();
        AssignShiftCommand.NotifyCanExecuteChanged();
        InsertUrgentOrderCommand.NotifyCanExecuteChanged();
        HandoverCommand.NotifyCanExecuteChanged();
        CancelRouteCommand.NotifyCanExecuteChanged();
        InterruptRouteCommand.NotifyCanExecuteChanged();
        if (value is not null)
            _ = LoadRouteDetailAsync(value.Id);
    }

    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadRoutesAsync();

    partial void OnSelectedDateChanged(DateTime? value) => _ = LoadRoutesAsync();

    partial void OnSearchTextChanged(string value)
    {
        var previousId = SelectedRoute?.Id;
        ApplyFilter();
        if (previousId is { } id)
            SelectedRoute = Routes.FirstOrDefault(r => r.Id == id);
    }

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
            IEnumerable<RouteListItem> items = result?.Items ?? [];

            if (SelectedStatusFilter == AllFilter)
                items = items.Where(r => r.Status != "Interrupted");

            _allRoutes = items.ToList();
            ApplyFilter();

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

    private void ApplyFilter()
    {
        var query = SearchText?.Trim();
        IEnumerable<RouteListItem> items = _allRoutes;

        if (!string.IsNullOrEmpty(query))
            items = items.Where(r => MatchesSearch(r, query));

        Routes = new ObservableCollection<RouteListItem>(items);
    }

    private static bool MatchesSearch(RouteListItem route, string query) =>
        Contains(route.Status, query)
        || Contains(route.DriverName, query)
        || Contains(route.WarehouseName, query)
        || Contains(route.Date?.ToString("dd.MM.yyyy"), query);

    private static bool Contains(string? value, string query) =>
        value is not null && value.Contains(query, StringComparison.OrdinalIgnoreCase);

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

    [RelayCommand(CanExecute = nameof(CanOptimize))]
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

    [RelayCommand(CanExecute = nameof(CanAssignShift))]
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

    [RelayCommand(CanExecute = nameof(CanInsertUrgentOrder))]
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

    [RelayCommand(CanExecute = nameof(CanHandover))]
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

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task CancelRouteAsync()
    {
        if (SelectedRoute is null)
            return;

        if (!_dialogService.ShowConfirm(
            "Cancel this route? Its orders will be returned to the pool.", "Confirm cancel"))
            return;

        try
        {
            await _apiHttpClient.PostAsync($"api/routes/{SelectedRoute.Id}/cancel");
            await LoadRoutesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to cancel route.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanInterrupt))]
    private async Task InterruptRouteAsync()
    {
        if (SelectedRoute is null)
            return;

        if (!_dialogService.ShowConfirm(
            "Interrupt this route? The driver will stop and remaining stops stay unassigned.", "Confirm interrupt"))
            return;

        try
        {
            await _apiHttpClient.PostAsync($"api/routes/{SelectedRoute.Id}/interrupt");
            await LoadRoutesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to interrupt route.";
        }
    }

    [RelayCommand]
    private void ViewOrderProof(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return;

        var historyViewModel = new OrderDeliveryHistoryViewModel(_apiHttpClient);
        _ = historyViewModel.LoadAsync(orderId);
        _dialogService.ShowDeliveryHistoryDialog(historyViewModel);
    }

    private void OnRouteStarted(RouteStartedEvent e) => RunOnUi(() => _ = LoadRoutesAsync());

    private void OnStopChanged(StopEvent e) => RunOnUi(() =>
    {
        if (SelectedRoute?.Id == e.RouteId)
            _ = LoadRouteDetailAsync(e.RouteId);
        _ = LoadRoutesAsync();
    });

    private void OnRouteChanged(RouteChangedEvent e) => RunOnUi(() =>
    {
        if (SelectedRoute?.Id == e.RouteId)
            _ = LoadRouteDetailAsync(e.RouteId);
        _ = LoadRoutesAsync();
    });

    private void OnDriverLocation(DriverLocationEvent e) => RunOnUi(() =>
    {
        if (SelectedRoute?.Id != e.RouteId)
            return;

        DriverLat = e.Latitude;
        DriverLng = e.Longitude;
    });

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(action);
    }

    public void Dispose()
    {
        if (_routeHub is not null)
        {
            _routeHub.RouteStarted -= OnRouteStarted;
            _routeHub.StopCompleted -= OnStopChanged;
            _routeHub.StopFailed -= OnStopChanged;
            _routeHub.StopSkipped -= OnStopChanged;
            _routeHub.RouteChanged -= OnRouteChanged;
            _routeHub.DriverLocation -= OnDriverLocation;
        }

        GC.SuppressFinalize(this);
    }
}
