namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class OrderListItem
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string CargoType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Weight { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CustomerName { get; set; }
}
