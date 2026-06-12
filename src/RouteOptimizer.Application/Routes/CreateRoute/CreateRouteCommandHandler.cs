using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.CreateRoute;

public class CreateRouteCommandHandler : IRequestHandler<CreateRouteCommand, Result<Guid>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IRouteRepository _routeRepository;

    public CreateRouteCommandHandler(IWarehouseRepository warehouseRepository, IOrderRepository orderRepository, IRouteRepository routeRepository)
    {
        _warehouseRepository = warehouseRepository;
        _orderRepository = orderRepository;
        _routeRepository = routeRepository;
    }

    public async Task<Result<Guid>> Handle(CreateRouteCommand request, CancellationToken ct)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, ct);

        if (warehouse is null)
            throw new NotFoundException("Warehouse is not found");

        var orders = await  _orderRepository.GetByIdsAsync(request.OrderIds, ct);

        if (orders.Count != request.OrderIds.Count)
            return Result<Guid>.Failure("One ore more orders not found");

        if (orders.Any(o => o.Status != OrderStatus.Created))
            return Result<Guid>.Failure("One or more orders are not available");

        if (orders.Any(o => o.WarehouseId != request.WarehouseId))
            return Result<Guid>.Failure("One or more orders are not from this warehouse");

        var routeResult = Route.Create(request.WarehouseId);

        if (routeResult.IsFailure)
            return Result<Guid>.Failure(routeResult.Error!);

        var route = routeResult.Value;

        foreach (var order in  orders )
        {
            var stopResult = Stop.Create(route!.Id, order.Address, order.Location, order.DeliveryWindow,
                sequenceNumber: 0, [order.Id]);

            if (stopResult.IsFailure)
                return Result<Guid>.Failure(stopResult.Error!);

            route!.AddStop(stopResult.Value!);
            order.AssignToRoute(route.Id);
        }

        await _routeRepository.AddAsync(route!, ct);
        return Result<Guid>.Success(route!.Id);
    }

}
