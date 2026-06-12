using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Warehouses.GetWarehouses;

public record GetWarehousesQuery() : IQuery<IReadOnlyList<WarehouseListItemDto>>;
