using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Orders.GetDeliveryAttempts;

public class GetOrderDeliveryAttemptsQueryHandler
    : IRequestHandler<GetOrderDeliveryAttemptsQuery, Result<IReadOnlyList<DeliveryAttemptDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;
    private readonly ICurrentUser _currentUser;

    public GetOrderDeliveryAttemptsQueryHandler(IOrderRepository orderRepository,
        IDeliveryAttemptRepository deliveryAttemptRepository, ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _deliveryAttemptRepository = deliveryAttemptRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<DeliveryAttemptDto>>> Handle(
        GetOrderDeliveryAttemptsQuery request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);

        if (order is null)
            return Result<IReadOnlyList<DeliveryAttemptDto>>.Failure("Order not found");

        if (_currentUser.WarehouseId.HasValue && order.WarehouseId != _currentUser.WarehouseId.Value)
            return Result<IReadOnlyList<DeliveryAttemptDto>>.Failure("Order not found");

        var attempts = await _deliveryAttemptRepository.GetByOrderIdAsync(request.OrderId, ct);

        var dtos = attempts
            .Select(a => new DeliveryAttemptDto(
                a.Id,
                a.OrderId,
                a.DriverId,
                a.AttemptedAt,
                a.Outcome.ToString(),
                a.FailureReason?.ToString(),
                a.Notes,
                a.PhotoKey,
                a.AttemptLocation.Latitude,
                a.AttemptLocation.Longitude))
            .ToList();

        return Result<IReadOnlyList<DeliveryAttemptDto>>.Success(dtos);
    }
}
