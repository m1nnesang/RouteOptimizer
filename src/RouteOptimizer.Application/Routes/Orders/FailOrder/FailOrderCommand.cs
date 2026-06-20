using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Orders.FailOrder;

public record FailOrderCommand(
    Guid DriverId,
    Guid RouteId,
    Guid StopId,
    Guid OrderId,
    double Latitude,
    double Longitude,
    FailureReason FailureReason,
    string? Notes,
    string? PhotoKey) : ICommand<Result>;
