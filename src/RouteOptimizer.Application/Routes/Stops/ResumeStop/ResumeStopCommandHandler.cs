using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Routes.Stops.ResumeStop;

public class ResumeStopCommandHandler : IRequestHandler<ResumeStopCommand, Result>
{
    private readonly IRouteRepository _routeRepository;

    public ResumeStopCommandHandler(IRouteRepository routeRepository) => _routeRepository = routeRepository;

    public async Task<Result> Handle(ResumeStopCommand request, CancellationToken ct)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, ct);

        if (route is null)
            throw new NotFoundException("Route is not found");

        if (route.Status != RouteStatus.InProgress)

            return Result.Failure("Route is done");

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId);

        if (stop is null)
            throw new NotFoundException("Stop is not found");

        if (stop.Status != StopStatus.Skipped)
            return Result.Failure("Stop cannot be resumed");

        try
        {
            stop.Resume();
        }

        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
