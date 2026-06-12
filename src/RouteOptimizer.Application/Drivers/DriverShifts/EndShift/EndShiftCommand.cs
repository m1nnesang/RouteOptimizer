using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Drivers.DriverShifts.EndShift;

public record EndShiftCommand(Guid ShiftId, Guid DriverId) : ICommand<Result>;
