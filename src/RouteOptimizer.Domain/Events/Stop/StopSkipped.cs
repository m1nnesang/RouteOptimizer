using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Stop;

public record StopSkipped(Guid StopId, Guid RouteId) : IDomainEvent;