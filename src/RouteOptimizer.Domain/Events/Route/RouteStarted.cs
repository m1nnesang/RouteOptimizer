using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Route;

public record RouteStarted(Guid RouteId, Guid ShiftId) : IDomainEvent;