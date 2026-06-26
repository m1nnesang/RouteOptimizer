using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class CreateShiftDialogViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public CreateShiftDialogViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
        _ = LoadAsync();
    }

    [ObservableProperty]
    public partial ObservableCollection<WarehouseListItem> Warehouses { get; set; } = [];

    [ObservableProperty]
    public partial WarehouseListItem? SelectedWarehouse { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DriverListItem> Drivers { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial DriverListItem? SelectedDriver { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<VehicleListItem> Vehicles { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial VehicleListItem? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial DateTime? ShiftDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool CanConfirm => SelectedDriver is not null && SelectedVehicle is not null;

    public DateOnly EffectiveDate => DateOnly.FromDateTime(ShiftDate ?? DateTime.Today);

    public CreateShiftRequest BuildRequest() =>
        new(SelectedDriver!.Id, SelectedVehicle!.Id, EffectiveDate);

    partial void OnSelectedWarehouseChanged(WarehouseListItem? value)
    {
        Vehicles = [];
        SelectedVehicle = null;
        if (value is not null)
            _ = LoadVehiclesAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var drivers = await _apiHttpClient.GetAsync<PagedResult<DriverListItem>>("api/drivers?pageSize=200");
            Drivers = new ObservableCollection<DriverListItem>(
                (drivers?.Items ?? []).Where(d => d.IsActive));

            var warehouses = await _apiHttpClient.GetAsync<List<WarehouseListItem>>("api/warehouses");
            Warehouses = new ObservableCollection<WarehouseListItem>(warehouses ?? []);
            SelectedWarehouse = Warehouses.FirstOrDefault();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load drivers or warehouses. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadVehiclesAsync()
    {
        if (SelectedWarehouse is null)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var vehicles = await _apiHttpClient.GetAsync<List<VehicleListItem>>(
                $"api/vehicles?warehouseId={SelectedWarehouse.Id}");
            Vehicles = new ObservableCollection<VehicleListItem>(vehicles ?? []);
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
}
