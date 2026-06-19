namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class RouteDetail
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? AssignedShiftId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<RouteStop> Stops { get; set; } = [];
}

public class RouteStop
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<Guid> OrderIds { get; set; } = [];
    public List<StopOrder> Orders { get; set; } = [];

    public int Position => Sequence + 1;
    public int OrdersCount => OrderIds.Count;
    public bool HasMultipleOrders => OrdersCount > 1;
    public string DeliveryWindow => string.Empty;
}

public class StopOrder
{
    public Guid OrderId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Apartment { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public string ApartmentDisplay => string.IsNullOrWhiteSpace(Apartment) ? "—" : $"ap. {Apartment}";
}
