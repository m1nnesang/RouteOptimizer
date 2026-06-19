using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class WarehousesViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;

    public WarehousesViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService)
    {
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        _ = LoadWarehousesAsync();
    }

    [ObservableProperty]
    public partial ObservableCollection<WarehouseListItem> Warehouses { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedWarehouse))]
    public partial WarehouseListItem? SelectedWarehouse { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasSelectedWarehouse => SelectedWarehouse is not null;

    [RelayCommand]
    private async Task LoadWarehousesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _apiHttpClient.GetAsync<List<WarehouseListItem>>("api/warehouses");
            Warehouses = new ObservableCollection<WarehouseListItem>(result ?? []);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load warehouses. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateWarehouseAsync()
    {
        var dialogViewModel = new WarehouseEditDialogViewModel();
        if (_dialogService.ShowWarehouseEditDialog(dialogViewModel) != true)
            return;

        try
        {
            await _apiHttpClient.PostAsync<CreateWarehouseRequest, Guid>(
                "api/warehouses", dialogViewModel.BuildCreateRequest());
            await LoadWarehousesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to create warehouse.";
        }
    }

    [RelayCommand]
    private async Task EditWarehouseAsync()
    {
        if (SelectedWarehouse is null)
            return;

        var dialogViewModel = new WarehouseEditDialogViewModel(SelectedWarehouse);
        if (_dialogService.ShowWarehouseEditDialog(dialogViewModel) != true)
            return;

        try
        {
            await _apiHttpClient.PutAsync(
                $"api/warehouses/{SelectedWarehouse.Id}", dialogViewModel.BuildUpdateRequest());
            await LoadWarehousesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to update warehouse.";
        }
    }

    [RelayCommand]
    private async Task DeleteWarehouseAsync()
    {
        if (SelectedWarehouse is null)
            return;

        if (!_dialogService.ShowConfirm($"Delete warehouse \"{SelectedWarehouse.Name}\"?", "Confirm delete"))
            return;

        try
        {
            await _apiHttpClient.DeleteAsync($"api/warehouses/{SelectedWarehouse.Id}");
            await LoadWarehousesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to delete warehouse.";
        }
    }
}
