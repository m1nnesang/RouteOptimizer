using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.Orders.DeliverOrder;

public record DeliverOrderCommand(
    Guid RouteId,
    Guid StopId,
    Guid OrderId,
    double? Latitude = null,
    double? Longitude = null,
    string? PhotoKey = null) : ICommand<Result>;
