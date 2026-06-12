using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Warehouses.DeleteWarehouse;

public record DeleteWarehouseCommand(Guid Id) : ICommand<Result>;
