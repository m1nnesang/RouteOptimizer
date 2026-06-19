using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Geocoding.GetAddressSuggestions;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/geocoding")]
public class GeocodingController : ControllerBase
{
    private readonly IMediator _mediator;

    public GeocodingController(IMediator mediator) => _mediator = mediator;

    [Authorize(Roles = "Dispatcher")]
    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAddressSuggestionsQuery(q ?? string.Empty), ct);
        return Ok(result);
    }
}
