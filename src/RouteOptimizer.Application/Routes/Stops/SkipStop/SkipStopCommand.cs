using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.Stops.SkipStop;

public record SkipStopCommand(Guid RouteId, Guid StopId) : ICommand<Result>;
