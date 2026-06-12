using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;

namespace RouteOptimizer.Application.Drivers.GetDrivers;

public record GetDriversQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<DriverListItemDto>>;
