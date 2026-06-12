using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Orders.CancelOrder;

public record CancelOrderCommand(Guid OrderId) : ICommand<Result>;
