namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class ShiftListItem
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;

    public string ShiftStatus => EndedAt is not null
        ? "Ended"
        : StartedAt is not null ? "Active" : "Scheduled";
}
