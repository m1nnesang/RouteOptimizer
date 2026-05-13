using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Events.Stop;

public record StopCompleted(Guid StopId, Guid RouteId , bool IsPartial) : IDomainEvent;