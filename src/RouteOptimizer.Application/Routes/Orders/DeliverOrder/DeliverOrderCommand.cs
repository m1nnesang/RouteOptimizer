using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.Orders.DeliverOrder;

public record DeliverOrderCommand(Guid RouteId, Guid StopId, Guid OrderId) : ICommand<Result>;
