using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.InterruptRoute;

public class InterruptRouteCommandHandler : IRequestHandler<InterruptRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;

    public InterruptRouteCommandHandler(IRouteRepository routeRepository) =>
        _routeRepository = routeRepository;

    public async Task<Result> Handle(InterruptRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        try
        {
            route.Interrupt();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
