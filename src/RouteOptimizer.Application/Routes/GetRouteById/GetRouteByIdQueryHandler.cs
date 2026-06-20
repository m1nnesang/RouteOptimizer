using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Routes.Stops;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Application.Routes.GetRouteById;

public class GetRouteByIdQueryHandler : IRequestHandler<GetRouteByIdQuery, Result<RouteDto>>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;

    public GetRouteByIdQueryHandler(IRouteRepository routeRepository, IOrderRepository orderRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<RouteDto>> Handle(GetRouteByIdQuery request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route is not found");

        if (_currentUser.WarehouseId.HasValue && route.WarehouseId != _currentUser.WarehouseId.Value)
            throw new NotFoundException("Route is not found");

        var orderIds = route.Stops.SelectMany(s => s.Orders).Distinct().ToList();
        var ordersById = (await _orderRepository.GetByIdsAsync(orderIds, ct)).ToDictionary(o => o.Id);

        var stops = route.Stops.Select(s => new StopDto(s.Id, s.Sequence, s.Address.City, s.Address.Street,
            s.Location.Latitude, s.Location.Longitude, s.Status.ToString(), s.Orders,
            s.Orders.Select(id => MapOrder(id, ordersById)).ToList())).ToList();

        var routeDto = new RouteDto(
            route.Id,
            route.WarehouseId,
            route.AssignedShiftId,
            route.Status.ToString(),
            stops
        );

        return Result<RouteDto>.Success(routeDto);
    }

    private static StopOrderDto MapOrder(Guid orderId, IReadOnlyDictionary<Guid, Order> ordersById)
    {
        if (!ordersById.TryGetValue(orderId, out var order))
            return new StopOrderDto(orderId, string.Empty, null, string.Empty, string.Empty, "Unknown");

        var (type, recipient) = order switch
        {
            BusinessOrder business => ("Business", business.CompanyName),
            IndividualOrder individual => ("Individual", individual.CustomerName),
            _ => (string.Empty, string.Empty)
        };

        return new StopOrderDto(orderId, recipient, order.Address.Apartment, type, order.Number.Value, order.Status.ToString());
    }
}
