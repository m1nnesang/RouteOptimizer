using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;
    private readonly IRouteHubService _routeHubService;
    private readonly ISessionNotifier _sessionNotifier;

    private OrdersViewModel? _ordersViewModel;
    private RoutesViewModel? _routesViewModel;
    private DriversViewModel? _driversViewModel;
    private VehiclesViewModel? _vehiclesViewModel;
    private WarehousesViewModel? _warehousesViewModel;
    private DashboardViewModel? _dashboardViewModel;

    public MainViewModel(IAuthService authService, IApiHttpClient apiHttpClient,
        IDialogService dialogService, IRouteHubService routeHubService,
        ISessionNotifier sessionNotifier)
    {
        _authService = authService;
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        _routeHubService = routeHubService;
        _sessionNotifier = sessionNotifier;
        _sessionNotifier.SessionExpired += OnSessionExpired;
        ShowOrders();
    }

    public event Action? LogoutRequested;

    [ObservableProperty]
    public partial ObservableObject? CurrentViewModel { get; set; }

    [ObservableProperty]
    public partial bool IsSessionExpired { get; set; }

    [ObservableProperty]
    public partial string ActiveTab { get; set; } = "Orders";

    private void OnSessionExpired() => RunOnUi(() => IsSessionExpired = true);

    [RelayCommand]
    private async Task ReLoginAsync()
    {
        IsSessionExpired = false;
        _authService.Logout();
        try { await _routeHubService.StopAsync(); } catch { }
        LogoutRequested?.Invoke();
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(action);
    }

    [RelayCommand]
    private void ShowOrders()
    {
        CurrentViewModel = _ordersViewModel ??= new OrdersViewModel(_apiHttpClient, _dialogService);
        ActiveTab = "Orders";
    }
    [RelayCommand]
    private void ShowRoutes()
    {
        CurrentViewModel = _routesViewModel ??= new RoutesViewModel(_apiHttpClient, _dialogService, _routeHubService);
        ActiveTab = "Routes";
    }
    [RelayCommand]
    private void ShowDrivers()
    {
        CurrentViewModel = _driversViewModel ??= new DriversViewModel(_apiHttpClient, _dialogService);
        ActiveTab = "Drivers";
    }
    [RelayCommand]
    private void ShowVehicles()
    {
        CurrentViewModel = _vehiclesViewModel ??= new VehiclesViewModel(_apiHttpClient, _dialogService);
        ActiveTab = "Vehicles";
    }
    [RelayCommand]
    private void ShowWarehouses()
    {
        CurrentViewModel = _warehousesViewModel ??= new WarehousesViewModel(_apiHttpClient, _dialogService);
        ActiveTab = "Warehouses";
    }
    [RelayCommand]
    private void ShowDashboard()
    {
        CurrentViewModel = _dashboardViewModel ??= new DashboardViewModel(_apiHttpClient);
        ActiveTab = "Dashboard";
    }
}
