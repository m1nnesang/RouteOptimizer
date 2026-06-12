using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.Stops.CompleteStop;

public record CompleteStopCommand(Guid RouteId, Guid StopId, bool IsPartial) : ICommand<Result>;
