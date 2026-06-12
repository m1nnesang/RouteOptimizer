using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Stops.FailDelivery;

public record FailDeliveryCommand(Guid DriverId, Guid RouteId, Guid StopId, double Latitude, double Longitude, FailureReason FailureReason, string? Notes, string? PhotoKey  ) : ICommand<Result>;
