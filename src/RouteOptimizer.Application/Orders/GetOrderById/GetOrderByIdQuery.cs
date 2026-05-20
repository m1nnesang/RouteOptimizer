using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Orders.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IQuery<Result<OrderDto>>;