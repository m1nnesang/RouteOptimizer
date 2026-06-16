using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.VIewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IApiHttpClient _apiHttpClient;

    public MainViewModel(IAuthService authService, IApiHttpClient apiHttpClient)
    {
        _authService = authService;
        _apiHttpClient = apiHttpClient;
        ShowOrders();
    }

    [ObservableProperty]
    public partial ObservableObject? CurrentViewModel { get; set; }

    [RelayCommand]
    private void ShowOrders() => CurrentViewModel = new OrdersViewModel(_apiHttpClient);
    [RelayCommand]
    private void ShowRoutes() => CurrentViewModel = new RoutesViewModel(_apiHttpClient);
    [RelayCommand]
    private void ShowDrivers() => CurrentViewModel = new DriversViewModel(_apiHttpClient);

}
