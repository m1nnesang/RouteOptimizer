using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.CancelRoute;

public class CancelRouteCommandHandler : IRequestHandler<CancelRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;

    public CancelRouteCommandHandler(IRouteRepository routeRepository) =>
        _routeRepository = routeRepository;

    public async Task<Result> Handle(CancelRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
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
