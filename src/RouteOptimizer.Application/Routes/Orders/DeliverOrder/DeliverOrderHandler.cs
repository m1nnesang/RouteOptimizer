using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Orders.DeliverOrder;

public class DeliverOrderHandler : IRequestHandler<DeliverOrderCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;

    public DeliverOrderHandler(IRouteRepository routeRepository, IOrderRepository orderRepository)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(DeliverOrderCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (route.Status != RouteStatus.InProgress)
            return Result.Failure("Route is done");

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId);

        if (stop is null)
            throw new NotFoundException("Stop is not found");

        if (stop.Status != StopStatus.InProgress)
            return Result.Failure("Stop is not in progress");

        if (!stop.Orders.Contains(request.OrderId))
            throw new NotFoundException("Order is not in this stop");

        var stopOrders = await _orderRepository.GetByIdsAsync(stop.Orders, ct);
        var order = stopOrders.FirstOrDefault(o => o.Id == request.OrderId);

        if (order is null)
            throw new NotFoundException("Order is not found");

        try
        {
            order.MarkAsDelivered();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        StopResolution.ResolveIfComplete(stop, stopOrders);

        return Result.Success();
    }
}
