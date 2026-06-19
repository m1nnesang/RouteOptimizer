using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.CreateRoute;

public record CreateRouteCommand(IReadOnlyList<Guid> OrderIds, DateOnly Date) : ICommand<Result<Guid>>;
