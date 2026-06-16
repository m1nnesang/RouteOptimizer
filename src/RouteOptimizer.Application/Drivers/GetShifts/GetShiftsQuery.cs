using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;

namespace RouteOptimizer.Application.Drivers.GetShifts;

public record GetShiftsQuery(DateOnly? Date, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ShiftListItemDto>>;
