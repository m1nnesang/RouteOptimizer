using MediatR;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Orders.GetDeliveryAttempts;

public record GetOrderDeliveryAttemptsQuery(Guid OrderId)
    : IRequest<Result<IReadOnlyList<DeliveryAttemptDto>>>;
