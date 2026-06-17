using CommunityToolkit.Mvvm.ComponentModel;
using RouteOptimizer.Dispatcher.Wpf.Models;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class WarehouseEditDialogViewModel : ObservableObject
{
    public WarehouseEditDialogViewModel(WarehouseListItem? existing = null)
    {
        IsEditMode = existing is not null;
        if (existing is not null)
        {
            Name = existing.Name;
            City = existing.City;
            Street = existing.Street;
            Latitude = existing.Latitude;
            Longitude = existing.Longitude;
        }
    }

    public bool IsEditMode { get; }

    public string Title => IsEditMode ? "Edit Warehouse" : "Create Warehouse";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Street { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PostalCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Country { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Latitude { get; set; }

    [ObservableProperty]
    public partial double Longitude { get; set; }

    public bool CanConfirm =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(City) &&
        !string.IsNullOrWhiteSpace(Street);

    public CreateWarehouseRequest BuildCreateRequest() =>
        new(Name, City, Street, PostalCode, Country, Latitude, Longitude);

    public UpdateWarehouseRequest BuildUpdateRequest() =>
        new(Name, Street, City, PostalCode, Country);
}
