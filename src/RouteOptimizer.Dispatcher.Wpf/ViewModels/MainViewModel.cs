using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.VIewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;

    public MainViewModel(IAuthService authService, IApiHttpClient apiHttpClient, IDialogService dialogService)
    {
        _authService = authService;
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        ShowOrders();
    }

    [ObservableProperty]
    public partial ObservableObject? CurrentViewModel { get; set; }

    [RelayCommand]
    private void ShowOrders() => CurrentViewModel = new OrdersViewModel(_apiHttpClient, _dialogService);
    [RelayCommand]
    private void ShowRoutes() => CurrentViewModel = new RoutesViewModel(_apiHttpClient, _dialogService);
    [RelayCommand]
    private void ShowDrivers() => CurrentViewModel = new DriversViewModel(_apiHttpClient);
    [RelayCommand]
    private void ShowVehicles() => CurrentViewModel = new VehiclesViewModel(_apiHttpClient, _dialogService);
    [RelayCommand]
    private void ShowWarehouses() => CurrentViewModel = new WarehousesViewModel(_apiHttpClient, _dialogService);
}
