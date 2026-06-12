using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Drivers.DriverShifts.StartShift;

public class StartShiftCommandHandler : IRequestHandler<StartShiftCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverShiftRepository _driverShiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartShiftCommandHandler(IDriverShiftRepository driverShiftRepository,IUserRepository userRepository , IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork) => (_driverShiftRepository, _userRepository, _vehicleRepository, _unitOfWork) = (driverShiftRepository, userRepository , vehicleRepository, unitOfWork);

    public async Task<Result<Guid>> Handle(StartShiftCommand request, CancellationToken ct)
    {
        var driver = await _userRepository.GetByIdAsync(request.DriverId, ct);

        if (driver is null)
            throw new NotFoundException("Driver not found");

        if (driver.Role is not UserRole.Driver)
            return Result<Guid>.Failure("User is not a driver");

        var driverShift = await _driverShiftRepository.GetActiveShiftByDriverIdAsync(driver.Id, ct);

        if (driverShift is not null)
            return Result<Guid>.Failure("Driver already have a shift");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, ct);

        if (vehicle is null)
            throw new NotFoundException("Vehicle not found");

        if (vehicle.WarehouseId != request.WarehouseId)
            return Result<Guid>.Failure("Vehicle is not from this warehouse");

        var shift = DriverShift.Create(request.DriverId, request.VehicleId, request.WarehouseId, request.ShiftDate);

        if (shift.IsFailure)
            return Result<Guid>.Failure(shift.Error!);

        await _driverShiftRepository.AddAsync(shift.Value!, ct);

        return Result<Guid>.Success(shift.Value!.Id);
    }
}
