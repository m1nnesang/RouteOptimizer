using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class DriversViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;

    public DriversViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService)
    {
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        _ = LoadShiftsAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<ShiftListItem> Shifts { get; set; } = [];

    [ObservableProperty]
    public partial ShiftListItem? SelectedShift { get; set; }

    [ObservableProperty]
    public partial DateTime? FilterDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsEmpty => !IsLoading && !HasError && Shifts.Count == 0;

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

    [RelayCommand]
    private async Task CreateShiftAsync()
    {
        var dialogViewModel = new CreateShiftDialogViewModel(_apiHttpClient);
        if (_dialogService.ShowCreateShiftDialog(dialogViewModel) != true)
            return;

        try
        {
            await _apiHttpClient.PostAsync<CreateShiftRequest, Guid>(
                "api/drivers/shifts", dialogViewModel.BuildRequest());
            await LoadShiftsAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to create shift.";
        }
    }
}
