using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.AssignRoute;

public record AssignRouteCommand(Guid RouteId, Guid ShiftId) : ICommand<Result>;
