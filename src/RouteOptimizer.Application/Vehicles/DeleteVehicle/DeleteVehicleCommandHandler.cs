using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Vehicles.DeleteVehicle;

public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand, Result>
{
    private readonly IVehicleRepository _vehicleRepository;

    public DeleteVehicleCommandHandler(IVehicleRepository vehicleRepository) => _vehicleRepository = vehicleRepository;

    public async Task<Result> Handle(DeleteVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.Id, ct);
        if (vehicle is null)
            return Result.Failure("Vehicle not found");

        await _vehicleRepository.DeleteAsync(vehicle, ct);

        return Result.Success();
    }
}
