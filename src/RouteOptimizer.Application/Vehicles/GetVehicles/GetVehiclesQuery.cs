using MediatR;

namespace RouteOptimizer.Application.Vehicles.GetVehicles;

public record GetVehiclesQuery(Guid WarehouseId) : IRequest<IReadOnlyList<VehicleListItemDto>>;
