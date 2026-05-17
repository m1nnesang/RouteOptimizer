using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;

namespace RouteOptimizer.Application.Orders.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository) => _orderRepository = orderRepository;

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var result = await _orderRepository.GetByIdAsync(request.Id, ct);

        if (result is null)
            return Result<OrderDto>.Failure("Order not found");

        return Result<OrderDto>.Success(new OrderDto(result.Id, result.Address.City, result.Address.Street,
            result.Address.PostalCode, result.Address.Country, result.Location.Latitude, result.Location.Longitude,
            result.DeliveryWindow?.Start, result.DeliveryWindow?.End, (result as BusinessOrder)?.CompanyName, (result as BusinessOrder)?.ContactPerson , (result as IndividualOrder)?.CustomerName, result.Number.Value, result.Weight.Value, result.Volume.Value, result.CargoType.ToString(), result.Notes));
    }
}