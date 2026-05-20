using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Route;

public record RouteInterrupted(Guid RouteId) : IDomainEvent;