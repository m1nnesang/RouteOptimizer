using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Warehouses.UpdateWarehouse;

public record UpdateWarehouseCommand(Guid Id, string Name, string Street, string City, string PostalCode, string Country) : ICommand<Result>;
