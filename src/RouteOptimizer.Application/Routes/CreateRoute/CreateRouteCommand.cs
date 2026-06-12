using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.CreateRoute;

public record CreateRouteCommand(Guid WarehouseId, IReadOnlyList<Guid> OrderIds) : ICommand<Result<Guid>>;
