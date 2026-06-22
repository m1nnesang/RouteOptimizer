using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Analytics.GetDashboard;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Authorize(Roles = "Manager,Dispatcher")]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var query = new GetDashboardQuery(date ?? DateOnly.FromDateTime(DateTime.UtcNow));
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
