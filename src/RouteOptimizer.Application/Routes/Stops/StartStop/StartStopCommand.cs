using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.Stops.StartStop;

public record StartStopCommand(Guid RouteId, Guid StopId) : ICommand<Result>;
