using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public DashboardViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
        _selectedDate = DateTime.Today;
        _ = LoadAsync(CancellationToken.None);
    }

    [ObservableProperty]
    private DateTime _selectedDate;

    [ObservableProperty]
    private StopStatusBreakdown _stops = new();

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<FailureReasonCount> FailureBreakdown { get; } = [];
    public ObservableCollection<DriverPerformance> DriverPerformance { get; } = [];

    partial void OnSelectedDateChanged(DateTime value) => _ = LoadAsync(CancellationToken.None);

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            var date = DateOnly.FromDateTime(SelectedDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var result = await _apiHttpClient.GetAsync<DashboardResult>($"api/analytics/dashboard?date={date}", ct);

            Stops = result?.Stops ?? new StopStatusBreakdown();

            FailureBreakdown.Clear();
            foreach (var item in result?.FailureBreakdown ?? [])
                FailureBreakdown.Add(item);

            DriverPerformance.Clear();
            foreach (var item in result?.DriverPerformance ?? [])
                DriverPerformance.Add(item);
        }
        catch (SessionExpiredException)
        {
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load analytics. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
