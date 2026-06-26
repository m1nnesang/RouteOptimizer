using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Drivers.DriverShifts.CreateShift;
using RouteOptimizer.Application.Drivers.DriverShifts.EndShift;
using RouteOptimizer.Application.Drivers.DriverShifts.StartShift;
using RouteOptimizer.Application.Drivers.GetDrivers;
using RouteOptimizer.Application.Drivers.GetShifts;
using RouteOptimizer.Application.Drivers.InviteDriver;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.API.Controllers;


[ApiController]
[Authorize]
[Route("api/drivers")]
public class DriversController : ControllerBase
{
    private readonly IMediator _mediator;

    public DriversController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private IActionResult ToResponse(Result result) =>
        result.IsSuccess ? Ok() : BadRequest(result.Error);

    [Authorize(Roles = "Manager")]
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteDriverCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("shifts/start")]
    public async Task<IActionResult> StartShift([FromBody] StartShiftRequest request,
        CancellationToken ct)
    {
        var driverId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var warehouseId = Guid.Parse(User.FindFirstValue("warehouse_id")!);

        var command = new StartShiftCommand(driverId, request.VehicleId, warehouseId,
            request.ShiftDate);

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("shifts/{shiftId:guid}/end")]
    public async Task<IActionResult> EndShift([FromRoute] Guid shiftId, CancellationToken ct)
    {
        var driverId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(new EndShiftCommand(shiftId, driverId), ct);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [Authorize(Roles = "Dispatcher,Manager")]
    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Dispatcher,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDriversQuery(page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Dispatcher,Manager")]
    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetShiftsQuery(date, page, pageSize), ct);
        return Ok(result);
    }

    public record StartShiftRequest(Guid VehicleId, DateOnly ShiftDate);

}
