using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Auth.Login;
using RouteOptimizer.Application.Auth.RefreshToken;
using RouteOptimizer.Application.Auth.RevokeToken;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    private IActionResult ToResponse<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Unauthorized(result.Error);

    private IActionResult ToResponse(Result result) =>
        result.IsSuccess ? NoContent() : BadRequest(result.Error);


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return ToResponse(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return ToResponse(result);
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenCommand command , CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return ToResponse(result);
    }

}
