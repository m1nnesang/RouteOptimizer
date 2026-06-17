using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RouteOptimizer.Dispatcher.Wpf.Models;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class VehicleEditDialogViewModel : ObservableObject
{
    private static readonly string[] AllLicenseCategories = ["B", "BE", "C1", "C", "CE"];
    private static readonly string[] AllCargoTypes = ["General", "Refrigerated", "Fragile", "Hazardous"];

    public VehicleEditDialogViewModel(VehicleListItem? existing = null)
    {
        IsEditMode = existing is not null;
        LicenseCategories = new ObservableCollection<string>(AllLicenseCategories);
        CargoTypes = new ObservableCollection<SelectableCargoType>(
            AllCargoTypes.Select(c => new SelectableCargoType { Name = c }));

        if (existing is not null)
        {
            Type = existing.Type;
            MaxWeightKg = existing.MaxWeightKg;
            MaxVolumeM3 = existing.MaxVolumeM3;
            SelectedLicenseCategory = existing.LicenseCategory;
            foreach (var cargo in CargoTypes)
                cargo.IsSelected = existing.AllowedCargoTypes.Contains(cargo.Name);
        }
        else
        {
            SelectedLicenseCategory = AllLicenseCategories[0];
        }
    }

    public bool IsEditMode { get; }

    public string Title => IsEditMode ? "Edit Vehicle" : "Create Vehicle";

    public ObservableCollection<string> LicenseCategories { get; }

    public ObservableCollection<SelectableCargoType> CargoTypes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Type { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal MaxWeightKg { get; set; }

    [ObservableProperty]
    public partial decimal MaxVolumeM3 { get; set; }

    [ObservableProperty]
    public partial string SelectedLicenseCategory { get; set; } = string.Empty;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Type);

    private IReadOnlyList<string> SelectedCargoTypes =>
        CargoTypes.Where(c => c.IsSelected).Select(c => c.Name).ToList();

    public CreateVehicleRequest BuildCreateRequest(Guid warehouseId) =>
        new(warehouseId, Type, MaxWeightKg, MaxVolumeM3, SelectedLicenseCategory, SelectedCargoTypes);

    public UpdateVehicleRequest BuildUpdateRequest() =>
        new(Type, MaxWeightKg, MaxVolumeM3, SelectedLicenseCategory, SelectedCargoTypes);
}

public partial class SelectableCargoType : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string Name { get; init; } = string.Empty;
}
