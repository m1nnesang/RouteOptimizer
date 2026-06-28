using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.CancelRoute;

public class CancelRouteCommandHandler : IRequestHandler<CancelRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;

    public CancelRouteCommandHandler(IRouteRepository routeRepository, IOrderRepository orderRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route not found");

        try
        {
            route.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await ReturnOrdersToPoolAsync(route, ct);

        return Result.Success();
    }

    private async Task ReturnOrdersToPoolAsync(Domain.Entities.Route.Route route, CancellationToken ct)
    {
        var orderIds = route.Stops.SelectMany(s => s.Orders).Distinct().ToList();

        if (orderIds.Count == 0)
            return;

        var orders = await _orderRepository.GetByIdsAsync(orderIds, ct);

        foreach (var order in orders.Where(o => o.Status is OrderStatus.AssignedToRoute or OrderStatus.InTransit))
            order.ReturnToPool();
    }
}
