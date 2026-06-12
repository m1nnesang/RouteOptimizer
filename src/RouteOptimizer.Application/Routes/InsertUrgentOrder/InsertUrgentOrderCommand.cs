using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.InsertUrgentOrder;

public record InsertUrgentOrderCommand(Guid RouteId, Guid OrderId) : ICommand<Result>;
