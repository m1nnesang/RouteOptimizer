using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.DeliveryPhotos.CreateUploadUrl;
using RouteOptimizer.Application.DeliveryPhotos.GetPhotoUrl;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/delivery-photos")]
public class DeliveryPhotosController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeliveryPhotosController(IMediator mediator) => _mediator = mediator;

    [Authorize(Roles = "Driver")]
    [HttpPost("upload-url")]
    public async Task<IActionResult> CreateUploadUrl(CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDeliveryPhotoUploadUrlCommand(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Dispatcher,Manager,Driver")]
    [HttpGet("{photoKey}")]
    public async Task<IActionResult> GetPhotoUrl([FromRoute] string photoKey, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeliveryPhotoUrlQuery(photoKey), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
