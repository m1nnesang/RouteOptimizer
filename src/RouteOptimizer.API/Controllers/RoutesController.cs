using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Routes;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoutesController(IMediator mediator) => _mediator = mediator;

    [Authorize]
    [HttpPost("{routeId}/optimize")]
    public async Task<ActionResult<OptimizeRouteResult>> Optimize([FromRoute] Guid routeId, [FromBody] OptimizeRouteRequest request, CancellationToken ct)
    {
        var command = new OptimizeRouteCommand(routeId, request.RouteDate);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    public record OptimizeRouteRequest(DateOnly RouteDate);
 }
