using MediatR;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Vehicles.GetVehicles;

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, IReadOnlyList<VehicleListItemDto>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public GetVehiclesQueryHandler(IVehicleRepository vehicleRepository) => _vehicleRepository = vehicleRepository;

    public async Task<IReadOnlyList<VehicleListItemDto>> Handle(GetVehiclesQuery request, CancellationToken ct)
    {
        var vehicles = await _vehicleRepository.GetByWarehouseIdAsync(request.WarehouseId, ct);

        return vehicles.Select(v => new VehicleListItemDto(
            v.Id,
            v.WarehouseId,
            v.Type,
            v.MaxWeightKg.Value,
            v.MaxVolumeM3.Value,
            v.LicenseCategory,
            v.AllowedCargoTypes)).ToList();
    }
}
