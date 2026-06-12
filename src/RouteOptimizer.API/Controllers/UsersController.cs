using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Users.DeactivateUser;


namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [Authorize(Roles = "Manager")]
    [HttpPost("{userId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute]Guid userId, CancellationToken ct)
    {
        var command = new DeactivateUserCommand(userId);

        var user = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (user == userId)
            return BadRequest("You cannot deactivate yourself");

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
