using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Warehouses.UpdateWarehouse;

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, Result>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IGeocodingService _geocodingService;

    public UpdateWarehouseCommandHandler(IWarehouseRepository warehouseRepository, IGeocodingService geocodingService)
    {
        _warehouseRepository = warehouseRepository;
        _geocodingService = geocodingService;
    }

    public async Task<Result> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, ct);
        if (warehouse is null)
            return Result.Failure("Warehouse not found");

        var address = Address.Create(request.Street, request.City, request.PostalCode, request.Country);
        if (address.IsFailure) return Result.Failure(address.Error ?? "Address is invalid");

        var location = await _geocodingService.GeocodeAsync(address.Value!, ct);
        if (location.IsFailure) return Result.Failure(location.Error ?? "Geocoding failed");

        var updateResult = warehouse.Update(request.Name, address.Value!, location.Value!);
        if (updateResult.IsFailure) return updateResult;

        await _warehouseRepository.UpdateAsync(warehouse, ct);

        return Result.Success();
    }
}
