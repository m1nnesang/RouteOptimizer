using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Warehouses.CreateWarehouse;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IGeocodingService _geocodingService;

    public CreateWarehouseCommandHandler(IWarehouseRepository warehouseRepository, IGeocodingService geocodingService)
    {
        _warehouseRepository = warehouseRepository;
        _geocodingService = geocodingService;
    }

    public async Task<Result<Guid>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var address = Address.Create(request.Street, request.City, request.PostalCode,request.Country );
        if (address.IsFailure) return Result<Guid>.Failure(address.Error ?? "Address is invalid");

        var location = await _geocodingService.GeocodeAsync(address.Value!, ct);
        if (location.IsFailure) return Result<Guid>.Failure(location.Error ?? "Geocoding failed");

        var warehouse = Warehouse.Create(request.Name, address.Value!, location.Value!);

        if (warehouse.IsFailure) return Result<Guid>.Failure(warehouse.Error ?? "failed to create warehouse");

        await _warehouseRepository.AddAsync(warehouse.Value!, ct);

        return Result<Guid>.Success(warehouse.Value!.Id);
    }
}
