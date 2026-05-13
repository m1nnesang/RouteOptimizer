using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Order;

public record OrderCreated(Guid OrderId, Guid WarehouseId) : IDomainEvent;