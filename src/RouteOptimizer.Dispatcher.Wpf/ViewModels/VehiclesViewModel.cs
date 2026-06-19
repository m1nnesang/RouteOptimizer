using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class VehiclesViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;

    public VehiclesViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService)
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
    public partial ObservableCollection<VehicleListItem> Vehicles { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedVehicle))]
    public partial VehicleListItem? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasSelectedWarehouse => SelectedWarehouse is not null;

    public bool HasSelectedVehicle => SelectedVehicle is not null;

    partial void OnSelectedWarehouseChanged(WarehouseListItem? value)
    {
        Vehicles = [];
        SelectedVehicle = null;
        if (value is not null)
            _ = LoadVehiclesAsync();
    }

    [RelayCommand]
    private async Task LoadWarehousesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _apiHttpClient.GetAsync<List<WarehouseListItem>>("api/warehouses");
            Warehouses = new ObservableCollection<WarehouseListItem>(result ?? []);
            SelectedWarehouse = Warehouses.FirstOrDefault();
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
    private async Task LoadVehiclesAsync()
    {
        if (SelectedWarehouse is null)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _apiHttpClient.GetAsync<List<VehicleListItem>>(
                $"api/vehicles?warehouseId={SelectedWarehouse.Id}");
            Vehicles = new ObservableCollection<VehicleListItem>(result ?? []);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load vehicles.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateVehicleAsync()
    {
        if (SelectedWarehouse is null)
            return;

        var dialogViewModel = new VehicleEditDialogViewModel();
        if (_dialogService.ShowVehicleEditDialog(dialogViewModel) != true)
            return;

        try
        {
            await _apiHttpClient.PostAsync<CreateVehicleRequest, Guid>(
                "api/vehicles", dialogViewModel.BuildCreateRequest(SelectedWarehouse.Id));
            await LoadVehiclesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to create vehicle.";
        }
    }

    [RelayCommand]
    private async Task EditVehicleAsync()
    {
        if (SelectedVehicle is null)
            return;

        var dialogViewModel = new VehicleEditDialogViewModel(SelectedVehicle);
        if (_dialogService.ShowVehicleEditDialog(dialogViewModel) != true)
            return;

        try
        {
            await _apiHttpClient.PutAsync(
                $"api/vehicles/{SelectedVehicle.Id}", dialogViewModel.BuildUpdateRequest());
            await LoadVehiclesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to update vehicle.";
        }
    }

    [RelayCommand]
    private async Task DeleteVehicleAsync()
    {
        if (SelectedVehicle is null)
            return;

        if (!_dialogService.ShowConfirm($"Delete vehicle \"{SelectedVehicle.Type}\"?", "Confirm delete"))
            return;

        try
        {
            await _apiHttpClient.DeleteAsync($"api/vehicles/{SelectedVehicle.Id}");
            await LoadVehiclesAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to delete vehicle.";
        }
    }
}
