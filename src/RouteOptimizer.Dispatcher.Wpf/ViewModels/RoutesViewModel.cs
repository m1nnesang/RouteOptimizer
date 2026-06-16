using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;
using RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

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

    public RoutesViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
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
    public partial ObservableCollection<string> StatusFilters { get; set; } = [];

    [ObservableProperty]
    public partial string SelectedStatusFilter { get; set; } = AllFilter;

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
        OptimizationComparisons = [];
        HasOptimizationResultFlag = false;
        SelectedRouteStops = [];
        if (value is not null)
            _ = LoadRouteDetailAsync(value.Id);
    }

    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadRoutesAsync();

    [RelayCommand]
    private async Task LoadRoutesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var url = "api/routes?pageSize=100";
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != AllFilter)
                url += $"&status={SelectedStatusFilter}";

            var result = await _apiHttpClient.GetAsync<PagedResult<RouteListItem>>(url);
            Routes = new ObservableCollection<RouteListItem>(result?.Items ?? []);
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
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load route details.";
        }
    }

    [RelayCommand]
    private async Task CreateRouteAsync()
    {
        var dialogViewModel = new CreateRouteDialogViewModel(_apiHttpClient);
        var dialog = new CreateRouteDialog(dialogViewModel) { Owner = Application.Current.MainWindow };

        if (dialog.ShowDialog() != true || dialogViewModel.SelectedOrderIds.Count == 0)
            return;

        try
        {
            await _apiHttpClient.PostAsync<CreateRouteRequest, Guid>(
                "api/routes", new CreateRouteRequest(dialogViewModel.SelectedOrderIds));
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
        var dialog = new AssignShiftDialog(dialogViewModel) { Owner = Application.Current.MainWindow };

        if (dialog.ShowDialog() != true || dialogViewModel.SelectedShift is null)
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
        var dialog = new InsertUrgentOrderDialog(dialogViewModel) { Owner = Application.Current.MainWindow };

        if (dialog.ShowDialog() != true || dialogViewModel.SelectedOrder is null)
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
        var dialog = new HandoverDialog(dialogViewModel) { Owner = Application.Current.MainWindow };

        if (dialog.ShowDialog() != true || !dialogViewModel.CanConfirm)
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
