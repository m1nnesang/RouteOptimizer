using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.VIewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IRouteHubService _routeHubService;

    public LoginViewModel(IAuthService authService, IRouteHubService routeHubService) =>
        (_authService, _routeHubService) = (authService, routeHubService);

    public event Action? LoginSucceeded;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    [RelayCommand]
    public async Task LoginAsync(CancellationToken ct)
    {
        IsLoading = true;
        OnPropertyChanged(nameof(IsNotLoading));

        try
        {
            var login = await _authService.LoginAsync(Email, Password, ct);

            if (login)
            {
                StartRouteHub();
                LoginSucceeded?.Invoke();
            }
            else
            {
                ErrorMessage = "Login failed";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Network error. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    private void StartRouteHub()
    {
        _ = Task.Run(async () =>
        {
            try { await _routeHubService.StartAsync(); }
            catch { }
        });
    }
}
