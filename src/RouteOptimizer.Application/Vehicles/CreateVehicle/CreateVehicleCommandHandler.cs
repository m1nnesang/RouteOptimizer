using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Vehicles.CreateVehicle;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public CreateVehicleCommandHandler(IVehicleRepository vehicleRepository, IWarehouseRepository warehouseRepository)
    {
        _vehicleRepository = vehicleRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<Guid>> Handle(CreateVehicleCommand request, CancellationToken ct)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, ct);
        if (warehouse is null)
            return Result<Guid>.Failure("Warehouse not found");

        var weight = Weight.Create(request.MaxWeightKg);
        if (weight.IsFailure) return Result<Guid>.Failure(weight.Error ?? "Invalid weight");

        var volume = Volume.Create(request.MaxVolumeM3);
        if (volume.IsFailure) return Result<Guid>.Failure(volume.Error ?? "Invalid volume");

        var vehicle = Vehicle.Create(request.WarehouseId, request.Type, weight.Value!, volume.Value!,
            request.LicenseCategory, request.AllowedCargoTypes);
        if (vehicle.IsFailure) return Result<Guid>.Failure(vehicle.Error ?? "Failed to create vehicle");

        await _vehicleRepository.AddAsync(vehicle.Value!, ct);

        return Result<Guid>.Success(vehicle.Value!.Id);
    }
}
