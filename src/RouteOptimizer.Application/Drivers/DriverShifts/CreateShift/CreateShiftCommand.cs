using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Drivers.DriverShifts.CreateShift;

public record CreateShiftCommand(Guid DriverId, Guid VehicleId, DateOnly ShiftDate) : ICommand<Result<Guid>>;
