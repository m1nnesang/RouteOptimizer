using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.CancelRoute;

public class CancelRouteCommandHandler : IRequestHandler<CancelRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;
    private readonly ICurrentUser _currentUser;

    public CancelRouteCommandHandler(IRouteRepository routeRepository, ICurrentUser currentUser)
    {
        _routeRepository = routeRepository;
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

        return Result.Success();
    }
}
