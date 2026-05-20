using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Application.Orders.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var result = await _orderRepository.GetByIdAsync(request.Id, ct);

        if (result is null)
            return Result<OrderDto>.Failure("Order not found");

        var dto = result switch
        {
            BusinessOrder b => new OrderDto(b.Id, b.Address.City, b.Address.Street, b.Address.PostalCode,
                b.Address.Country, b.Location.Latitude, b.Location.Longitude, b.DeliveryWindow?.Start,
                b.DeliveryWindow?.End, b.CompanyName, b.ContactPerson, null, b.Number.Value, b.Weight.Value,
                b.Volume.Value, b.CargoType.ToString(), b.Notes),

            IndividualOrder i => new OrderDto(i.Id, i.Address.City, i.Address.Street, i.Address.PostalCode,
                i.Address.Country, i.Location.Latitude, i.Location.Longitude, i.DeliveryWindow?.Start,
                i.DeliveryWindow?.End, null, null, i.CustomerName, i.Number.Value, i.Weight.Value, i.Volume.Value,
                i.CargoType.ToString(), i.Notes),
            _ => throw new InvalidOperationException("Invalid order type")
        };

        return Result<OrderDto>.Success(dto);
    }
}
