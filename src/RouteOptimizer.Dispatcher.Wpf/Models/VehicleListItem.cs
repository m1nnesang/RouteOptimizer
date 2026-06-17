namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class VehicleListItem
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string Type { get; set; } = "";
    public decimal MaxWeightKg { get; set; }
    public decimal MaxVolumeM3 { get; set; }
    public string LicenseCategory { get; set; } = "";
    public List<string> AllowedCargoTypes { get; set; } = [];

    public string AllowedCargoTypesDisplay => string.Join(", ", AllowedCargoTypes);
}
