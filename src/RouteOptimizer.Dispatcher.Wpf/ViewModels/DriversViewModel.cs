using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.VIewModels;

public partial class DriversViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public DriversViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
        _ = LoadShiftsAsync();
    }

    [ObservableProperty]
    public partial ObservableCollection<ShiftListItem> Shifts { get; set; } = [];

    [ObservableProperty]
    public partial ShiftListItem? SelectedShift { get; set; }

    [ObservableProperty]
    public partial DateTime? FilterDate { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnFilterDateChanged(DateTime? value) => _ = LoadShiftsAsync();

    [RelayCommand]
    private async Task LoadShiftsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var url = "api/drivers/shifts?pageSize=200";
            if (FilterDate is { } date)
                url += $"&date={DateOnly.FromDateTime(date):yyyy-MM-dd}";

            var result = await _apiHttpClient.GetAsync<PagedResult<ShiftListItem>>(url);
            Shifts = new ObservableCollection<ShiftListItem>(result?.Items ?? []);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load shifts. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearDateFilter() => FilterDate = null;
}
