using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Drivers.DriverShifts.StartShift;

public record StartShiftCommand(Guid DriverId, Guid VehicleId, Guid WarehouseId, DateOnly ShiftDate) : ICommand<Result<Guid>>;
