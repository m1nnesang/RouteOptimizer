using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Order;

public record OrderCancelled(Guid OrderId) : IDomainEvent;