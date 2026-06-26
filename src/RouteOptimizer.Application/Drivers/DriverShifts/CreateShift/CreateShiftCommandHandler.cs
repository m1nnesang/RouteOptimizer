using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Drivers.DriverShifts.CreateShift;

public class CreateShiftCommandHandler : IRequestHandler<CreateShiftCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverShiftRepository _driverShiftRepository;

    public CreateShiftCommandHandler(IDriverShiftRepository driverShiftRepository, IUserRepository userRepository,
        IVehicleRepository vehicleRepository) =>
        (_driverShiftRepository, _userRepository, _vehicleRepository) =
        (driverShiftRepository, userRepository, vehicleRepository);

    public async Task<Result<Guid>> Handle(CreateShiftCommand request, CancellationToken ct)
    {
        var driver = await _userRepository.GetByIdAsync(request.DriverId, ct);

        if (driver is null)
            throw new NotFoundException("Driver not found");

        if (driver.Role is not UserRole.Driver)
            return Result<Guid>.Failure("User is not a driver");

        var activeShift = await _driverShiftRepository.GetActiveShiftByDriverIdAsync(driver.Id, ct);

        if (activeShift is not null)
            return Result<Guid>.Failure("Driver already has an active shift");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, ct);

        if (vehicle is null)
            throw new NotFoundException("Vehicle not found");

        var shift = DriverShift.Create(request.DriverId, request.VehicleId, vehicle.WarehouseId, request.ShiftDate);

        if (shift.IsFailure)
            return Result<Guid>.Failure(shift.Error!);

        shift.Value!.Start();
        await _driverShiftRepository.AddAsync(shift.Value!, ct);

        return Result<Guid>.Success(shift.Value!.Id);
    }
}
