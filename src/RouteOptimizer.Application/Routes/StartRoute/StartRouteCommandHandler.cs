using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.StartRoute;

public class StartRouteCommandHandler : IRequestHandler<StartRouteCommand, Result>
{
    private readonly IRouteRepository _routeRepository;

    public StartRouteCommandHandler(IRouteRepository routeRepository) =>
        _routeRepository = routeRepository;

    public async Task<Result> Handle(StartRouteCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route not found");

        try
        {
            route.Start();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
