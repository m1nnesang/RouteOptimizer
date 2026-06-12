using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Vehicles.UpdateVehicle;

public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, Result>
{
    private readonly IVehicleRepository _vehicleRepository;

    public UpdateVehicleCommandHandler(IVehicleRepository vehicleRepository) => _vehicleRepository = vehicleRepository;

    public async Task<Result> Handle(UpdateVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.Id, ct);
        if (vehicle is null)
            return Result.Failure("Vehicle not found");

        var weight = Weight.Create(request.MaxWeightKg);
        if (weight.IsFailure) return Result.Failure(weight.Error ?? "Invalid weight");

        var volume = Volume.Create(request.MaxVolumeM3);
        if (volume.IsFailure) return Result.Failure(volume.Error ?? "Invalid volume");

        var updateResult = vehicle.Update(request.Type, weight.Value!, volume.Value!,
            request.LicenseCategory, request.AllowedCargoTypes);
        if (updateResult.IsFailure) return updateResult;

        await _vehicleRepository.UpdateAsync(vehicle, ct);

        return Result.Success();
    }
}
