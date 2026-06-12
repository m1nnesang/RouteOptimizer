using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Application.Orders.GetOrders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository) => _orderRepository = orderRepository;

    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (orders, totalCount) = await _orderRepository.GetAllAsync(request.Status, request.Date, skip, request.PageSize, ct);

        var items = orders.Select(result => result switch
        {
            BusinessOrder b => new OrderListItemDto(b.Id, b.Address.City, b.Address.Street, b.Status.ToString(), "Business", b.CargoType.ToString(), b.CreatedAt, b.Weight.Value, b.Number.Value, b.CompanyName, null),
            IndividualOrder i => new OrderListItemDto(i.Id, i.Address.City, i.Address.Street, i.Status.ToString(), "Individual", i.CargoType.ToString(), i.CreatedAt, i.Weight.Value, i.Number.Value, null, i.CustomerName),
            _ => throw new InvalidOperationException()
        }).ToList();

        return new PagedResult<OrderListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
