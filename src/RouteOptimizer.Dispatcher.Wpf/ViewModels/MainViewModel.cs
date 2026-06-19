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

    private OrdersViewModel? _ordersViewModel;
    private RoutesViewModel? _routesViewModel;
    private DriversViewModel? _driversViewModel;
    private VehiclesViewModel? _vehiclesViewModel;
    private WarehousesViewModel? _warehousesViewModel;

    public MainViewModel(IAuthService authService, IApiHttpClient apiHttpClient,
        IDialogService dialogService, IRouteHubService routeHubService)
    {
        _authService = authService;
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        _routeHubService = routeHubService;
        ShowOrders();
    }

    [ObservableProperty]
    public partial ObservableObject? CurrentViewModel { get; set; }

    [RelayCommand]
    private void ShowOrders() =>
        CurrentViewModel = _ordersViewModel ??= new OrdersViewModel(_apiHttpClient, _dialogService);
    [RelayCommand]
    private void ShowRoutes() =>
        CurrentViewModel = _routesViewModel ??= new RoutesViewModel(_apiHttpClient, _dialogService, _routeHubService);
    [RelayCommand]
    private void ShowDrivers() =>
        CurrentViewModel = _driversViewModel ??= new DriversViewModel(_apiHttpClient);
    [RelayCommand]
    private void ShowVehicles() =>
        CurrentViewModel = _vehiclesViewModel ??= new VehiclesViewModel(_apiHttpClient, _dialogService);
    [RelayCommand]
    private void ShowWarehouses() =>
        CurrentViewModel = _warehousesViewModel ??= new WarehousesViewModel(_apiHttpClient, _dialogService);
}
