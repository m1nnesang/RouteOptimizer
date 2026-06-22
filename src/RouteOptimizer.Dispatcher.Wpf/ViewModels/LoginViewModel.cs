using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

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

    [ObservableProperty]
    private bool _isResetMode;

    public bool IsLoginMode => !IsResetMode;

    partial void OnIsResetModeChanged(bool value) => OnPropertyChanged(nameof(IsLoginMode));

    [ObservableProperty]
    private string _resetEmail = "";

    [ObservableProperty]
    private string _resetToken = "";

    [ObservableProperty]
    private string _resetNewPassword = "";

    [ObservableProperty]
    private string _resetMessage = "";

    [RelayCommand]
    private void ShowReset()
    {
        IsResetMode = true;
        ErrorMessage = "";
        ResetMessage = "";
    }

    [RelayCommand]
    private void BackToLogin()
    {
        IsResetMode = false;
        ResetMessage = "";
    }

    [RelayCommand]
    private async Task RequestResetAsync(CancellationToken ct)
    {
        ResetMessage = "";
        try
        {
            await _authService.RequestPasswordResetAsync(ResetEmail, ct);
            ResetMessage = "If the email exists, a reset link has been sent.";
        }
        catch (HttpRequestException)
        {
            ResetMessage = "Network error. Please check your connection.";
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(CancellationToken ct)
    {
        ResetMessage = "";
        try
        {
            var ok = await _authService.ResetPasswordAsync(ResetToken, ResetNewPassword, ct);
            ResetMessage = ok
                ? "Password updated. You can sign in now."
                : "Invalid or expired reset token.";
            if (ok) IsResetMode = false;
        }
        catch (HttpRequestException)
        {
            ResetMessage = "Network error. Please check your connection.";
        }
    }

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
