using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.HandoverRoute;

public record HandoverRouteCommand(
    Guid RouteId,
    HandoverType Type,
    Guid? TargetShiftId,
    IReadOnlyList<ShiftStopsAssignment>? Assignments
) : ICommand<Result>;

public record ShiftStopsAssignment(Guid ShiftId, IReadOnlyList<Guid> StopIds);
