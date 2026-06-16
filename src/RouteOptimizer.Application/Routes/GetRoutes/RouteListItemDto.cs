namespace RouteOptimizer.Application.Routes.GetRoutes;

public record RouteListItemDto(Guid Id, Guid WarehouseId, string Status,int StopsCount ,Guid? AssignedShiftId, DateOnly? Date, string? DriverName, string WarehouseName);
