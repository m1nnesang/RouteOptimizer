namespace RouteOptimizer.Dispatcher.Wpf.Models;

public enum HandoverType
{
    TransferAll,
    Distribute,
    ReturnToPool
}

public record CreateRouteRequest(IReadOnlyList<Guid> OrderIds);

public record OptimizeRouteRequest(DateOnly RouteDate);

public record AssignRouteRequest(Guid ShiftId);

public record InsertUrgentOrderRequest(Guid OrderId);

public record ShiftStopsAssignment(Guid ShiftId, IReadOnlyList<Guid> StopIds);

public record HandoverRouteRequest(
    HandoverType Type,
    Guid? TargetShiftId,
    IReadOnlyList<ShiftStopsAssignment>? Assignments);
