using MediatR;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Orders.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;