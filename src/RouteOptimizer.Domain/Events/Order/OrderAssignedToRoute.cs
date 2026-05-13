using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Order;

public record OrderAssignedToRoute(Guid OrderId, Guid RouteId) : IDomainEvent;