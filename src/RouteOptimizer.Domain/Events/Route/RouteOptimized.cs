using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Route;

public record RouteOptimized(Guid RouteId) : IDomainEvent;