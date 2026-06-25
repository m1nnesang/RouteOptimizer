using MediatR;
using Microsoft.Extensions.Logging;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Stops.StartStop;

public class StartStopHandler : IRequestHandler<StartStopCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDriverShiftRepository _shiftRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<StartStopHandler> _logger;

    public StartStopHandler(IRouteRepository routeRepository, IOrderRepository orderRepository, IDriverShiftRepository shiftRepository, ICurrentUser currentUser, ILogger<StartStopHandler> logger)
    {
        _routeRepository = routeRepository;
        _orderRepository = orderRepository;
        _shiftRepository = shiftRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(StartStopCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route is not found");

        if (!await DriverRouteAccess.IsAssignedToDriverAsync(_shiftRepository, route, _currentUser.UserId, ct))
            throw new NotFoundException("Route is not found");

        if (route.Status != RouteStatus.InProgress)
            return Result.Failure("Route is not in progress");

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId);

        if (stop is null)
            throw new NotFoundException("Stop is not found");

        if (stop.Status != StopStatus.Pending)
            return Result.Failure("Stop cannot be started");

        try
        {
            stop.Start();
        }

        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        var orders = await _orderRepository.GetByIdsAsync(stop.Orders, ct);
        foreach (var order in orders)
        {
            try
            {
                order.MarkAsInTransit();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "Order {OrderId} could not be marked in transit while starting stop {StopId}",
                    order.Id, stop.Id);
            }
        }

        return Result.Success();
    }
}
