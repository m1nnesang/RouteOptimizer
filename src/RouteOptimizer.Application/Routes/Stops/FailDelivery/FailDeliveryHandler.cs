using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Routes.Stops.FailDelivery;

public class FailDeliveryHandler : IRequestHandler<FailDeliveryCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverShiftRepository _driverShiftRepository;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;

    public FailDeliveryHandler(IRouteRepository routeRepository , IDriverShiftRepository driverShiftRepository, IDeliveryAttemptRepository deliveryAttemptRepository) => (_routeRepository, _driverShiftRepository, _deliveryAttemptRepository) = (routeRepository, driverShiftRepository, deliveryAttemptRepository);

    public async Task<Result> Handle(FailDeliveryCommand request, CancellationToken ct)
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

        var shift = await _driverShiftRepository.GetByIdAsync(route.AssignedShiftId!.Value, ct);

        if (shift is null)
            throw new NotFoundException("Shift not found");

        if (shift.DriverId != request.DriverId)
            return Result.Failure("Access denied");

        Result<GeoCoordinate> coordResult = GeoCoordinate.Create(request.Latitude, request.Longitude);

        if (coordResult.IsFailure)
            return Result.Failure(coordResult.Error!);

        var location = coordResult.Value;

        foreach (var orderId in stop.Orders )
        {
            var result = DeliveryAttempt.Create(orderId, shift!.DriverId, route.Id, location!, DeliveryOutcome.Failed,
                request.FailureReason, request.Notes, request.PhotoKey);

            if (result.IsFailure)
                return Result.Failure(result.Error!);

            await _deliveryAttemptRepository.AddAsync(result.Value!, ct);
        }

        stop.Failed();

        return Result.Success();
    }
}
