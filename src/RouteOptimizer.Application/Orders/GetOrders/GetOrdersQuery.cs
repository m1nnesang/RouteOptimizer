using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Orders.GetOrders;

public record GetOrdersQuery(OrderStatus? Status, DateOnly? Date, int Page = 1, int PageSize = 20) : IQuery<PagedResult<OrderListItemDto>>;
