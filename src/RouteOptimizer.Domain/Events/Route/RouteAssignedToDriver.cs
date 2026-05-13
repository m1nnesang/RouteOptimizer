using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Route;

public record RouteAssignedToDriver(Guid RouteId, Guid ShiftId) : IDomainEvent;