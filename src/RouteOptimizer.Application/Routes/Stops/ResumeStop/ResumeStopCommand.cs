using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.Stops.ResumeStop;

public record ResumeStopCommand(Guid RouteId, Guid StopId) : ICommand<Result>;
