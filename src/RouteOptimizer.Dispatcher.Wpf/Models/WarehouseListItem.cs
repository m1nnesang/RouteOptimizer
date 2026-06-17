namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class WarehouseListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string Street { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
