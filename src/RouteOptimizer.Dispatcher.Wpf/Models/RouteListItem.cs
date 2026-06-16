namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class RouteListItem
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StopsCount { get; set; }
    public Guid? AssignedShiftId { get; set; }
    public DateOnly? Date { get; set; }
    public string? DriverName { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}
