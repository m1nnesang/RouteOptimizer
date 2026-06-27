using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.InterruptRoute;

public class InterruptRouteCommandHandler : IRequestHandler<InterruptRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;

    public InterruptRouteCommandHandler(IRouteRepository routeRepository, IDriverShiftRepository shiftRepository, IOrderRepository orderRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _shiftRepository = shiftRepository;
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(InterruptRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route not found");

        try
        {
            route.Interrupt();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await EndAssignedShiftAsync(route.AssignedShiftId, ct);
        await ReturnUndeliveredOrdersToPoolAsync(route, ct);

        return Result.Success();
    }

    private async Task ReturnUndeliveredOrdersToPoolAsync(Domain.Entities.Route.Route route, CancellationToken ct)
    {
        var orderIds = route.Stops.SelectMany(s => s.Orders).Distinct().ToList();

        if (orderIds.Count == 0)
            return;

        var orders = await _orderRepository.GetByIdsAsync(orderIds, ct);

        foreach (var order in orders.Where(o => o.Status is OrderStatus.AssignedToRoute or OrderStatus.InTransit))
            order.ReturnToPool();
    }

    private async Task EndAssignedShiftAsync(Guid? shiftId, CancellationToken ct)
    {
        if (shiftId is not { } id)
            return;

        var shift = await _shiftRepository.GetByIdAsync(id, ct);

        if (shift is { StartedAt: not null, EndedAt: null })
            shift.End();
    }
}
