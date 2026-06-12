using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Warehouses.CreateWarehouse;

public record CreateWarehouseCommand(string Name, string City, string Street, string PostalCode,string Country, double Latitude, double Longitude) : ICommand<Result<Guid>>;
