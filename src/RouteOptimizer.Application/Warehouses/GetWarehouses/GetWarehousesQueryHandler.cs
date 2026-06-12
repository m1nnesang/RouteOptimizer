using MediatR;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Warehouses.GetWarehouses;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseListItemDto>>
{
    private readonly IWarehouseRepository _warehouseRepository;

    public GetWarehousesQueryHandler(IWarehouseRepository warehouseRepository) => _warehouseRepository = warehouseRepository;

    public async Task<IReadOnlyList<WarehouseListItemDto>> Handle(GetWarehousesQuery request, CancellationToken ct)
    {
        var warehouse = await _warehouseRepository.GetAllAsync(ct);

        return warehouse.Select(result => new WarehouseListItemDto(result.Id, result.Name, result.Address.City, result.Address.Street, result.Location.Latitude, result.Location.Longitude)).ToList();
    }
}
