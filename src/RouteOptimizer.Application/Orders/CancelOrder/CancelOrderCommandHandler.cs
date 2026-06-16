using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Orders.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRouteRepository _routeRepository;
    private readonly ICurrentUser _currentUser;

    public CancelOrderCommandHandler(IOrderRepository orderRepository, IRouteRepository routeRepository, ICurrentUser currentUser) =>
        (_orderRepository, _routeRepository, _currentUser) = (orderRepository, routeRepository, currentUser);

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);

        if (order is null)
            return Result.Failure("Order not found");

        if (_currentUser.WarehouseId.HasValue && order.WarehouseId != _currentUser.WarehouseId.Value)
            return Result.Failure("Order not found");

        if (order.Status != OrderStatus.Created && order.Status != OrderStatus.AssignedToRoute)
            return Result.Failure("Order cannot be cancelled");

        if (order.AssignedRouteId is not null)
        {
            var route = await _routeRepository.GetByIdAsync(order.AssignedRouteId.Value, ct);

            if (route is not null)
            {
                var removeResult = route.RemoveCancelledOrder(order.Id);

                if (removeResult.IsFailure)
                    return removeResult;
            }
        }

        order.MarkAsCancelled();

        return Result.Success();
    }
}
