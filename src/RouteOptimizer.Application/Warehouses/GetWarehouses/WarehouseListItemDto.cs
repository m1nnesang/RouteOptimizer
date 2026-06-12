namespace RouteOptimizer.Application.Warehouses.GetWarehouses;

public record WarehouseListItemDto(Guid Id, string Name, string City, string Street, double Latitude, double Longitude);
