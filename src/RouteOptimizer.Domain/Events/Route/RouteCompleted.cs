using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Route;

public record RouteCompleted(Guid RouteId) : IDomainEvent;