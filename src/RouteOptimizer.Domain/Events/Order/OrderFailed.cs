using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Order;

public record OrderFailed(Guid OrderId, Guid RouteId) : IDomainEvent;