using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Route;

public record UrgentOrderInserted(Guid RouteId) : IDomainEvent;
