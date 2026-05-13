using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Stop;

public record StopFailed(Guid StopId, Guid RouteId) : IDomainEvent;