using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Routes.AssignRoute;
using RouteOptimizer.Application.Routes.CancelRoute;
using RouteOptimizer.Application.Routes.CompleteRoute;
using RouteOptimizer.Application.Routes.CreateRoute;
using RouteOptimizer.Application.Routes.GetMyRoutes;
using RouteOptimizer.Application.Routes.GetRouteById;
using RouteOptimizer.Application.Routes.GetRoutes;
using RouteOptimizer.Application.Routes.InterruptRoute;
using RouteOptimizer.Application.Routes.StartRoute;
using RouteOptimizer.Application.Routes.Stops.CompleteStop;
using RouteOptimizer.Application.Routes.Stops.FailDelivery;
using RouteOptimizer.Application.Routes.HandoverRoute;
using RouteOptimizer.Application.Routes.InsertUrgentOrder;
using RouteOptimizer.Application.Routes.Orders.DeliverOrder;
using RouteOptimizer.Application.Routes.Orders.FailOrder;
using RouteOptimizer.Application.Routes.Optimize;
using RouteOptimizer.Application.Routes.PushDriverLocation;
using RouteOptimizer.Application.Routes.Stops.ResumeStop;
using RouteOptimizer.Application.Routes.Stops.SkipStop;
using RouteOptimizer.Application.Routes.Stops.StartStop;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoutesController(IMediator mediator) => _mediator = mediator;

    #region Stops
    [Authorize]
    [HttpPost("{routeId}/optimize")]
    public async Task<ActionResult<OptimizeRouteResult>> Optimize([FromRoute] Guid routeId, [FromBody] OptimizeRouteRequest request, CancellationToken ct)
    {
        var command = new OptimizeRouteCommand(routeId, request.RouteDate);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }


    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/stops/{stopId}/start")]
    public async Task<IActionResult> StartStop([FromRoute] Guid routeId, [FromRoute] Guid stopId, CancellationToken ct)
    {
        var command = new StartStopCommand(routeId, stopId);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/stops/{stopId}/complete")]
    public async Task<IActionResult> CompleteStop([FromRoute] Guid routeId, [FromRoute] Guid stopId,[FromBody] CompleteStopRequest request ,CancellationToken ct)
    {
        var command = new CompleteStopCommand(routeId, stopId, request.IsPartial);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/stops/{stopId}/skip")]
    public async Task<IActionResult> SkipStop([FromRoute] Guid routeId, [FromRoute] Guid stopId, CancellationToken ct)
    {
        var command = new SkipStopCommand(routeId, stopId);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPatch("{routeId}/stops/{stopId}/resume")]
    public async Task<IActionResult> ResumeStop([FromRoute] Guid routeId, [FromRoute] Guid stopId, CancellationToken ct)
    {
        var command = new ResumeStopCommand(routeId, stopId);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/stops/{stopId}/fail")]
    public async Task<IActionResult> FailStop([FromRoute] Guid routeId, [FromRoute] Guid stopId, [FromBody] FailDeliveryRequest request ,CancellationToken ct)
    {
        var driverId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new FailDeliveryCommand(driverId ,routeId, stopId, request.Latitude, request.Longitude, request.FailureReason, request.Notes, request.PhotoKey);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/stops/{stopId}/orders/{orderId}/deliver")]
    public async Task<IActionResult> DeliverOrder([FromRoute] Guid routeId, [FromRoute] Guid stopId, [FromRoute] Guid orderId, CancellationToken ct)
    {
        var command = new DeliverOrderCommand(routeId, stopId, orderId);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/stops/{stopId}/orders/{orderId}/fail")]
    public async Task<IActionResult> FailOrder([FromRoute] Guid routeId, [FromRoute] Guid stopId, [FromRoute] Guid orderId, [FromBody] FailDeliveryRequest request, CancellationToken ct)
    {
        var driverId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new FailOrderCommand(driverId, routeId, stopId, orderId, request.Latitude, request.Longitude, request.FailureReason, request.Notes, request.PhotoKey);
        var result = await _mediator.Send(command, ct);

        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{routeId}/location")]
    public async Task<IActionResult> PushLocation([FromRoute] Guid routeId, [FromBody] PushLocationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new PushDriverLocationCommand(routeId, request.Latitude, request.Longitude), ct);
        return ToResponse(result);
    }
    #endregion


    #region Routes

    [Authorize(Roles = "Dispatcher,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetRoutes([FromQuery] RouteStatus? status, [FromQuery] DateOnly? date,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRoutesQuery(status, date, page, pageSize), ct);

        return Ok(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyRoutes([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyRoutesQuery(date), ct);

        return Ok(result);
    }

    [Authorize(Roles = "Dispatcher,Manager,Driver")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRouteById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRouteByIdQuery(id), ct);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRouteCommand(request.OrderIds, request.Date), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> AssignRoute([FromRoute] Guid id, [FromBody] AssignRouteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignRouteCommand(id, request.ShiftId), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Driver")]
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> StartRoute([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartRouteCommand(id), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Dispatcher,Driver")]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteRoute([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteRouteCommand(id), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("{id:guid}/interrupt")]
    public async Task<IActionResult> InterruptRoute([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new InterruptRouteCommand(id), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelRoute([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelRouteCommand(id), ct);
        return ToResponse(result);
    }

    #endregion


    [Authorize(Roles = "Dispatcher")]
    [HttpPost("{id:guid}/urgent-order")]
    public async Task<IActionResult> InsertUrgentOrder([FromRoute] Guid id, [FromBody] InsertUrgentOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new InsertUrgentOrderCommand(id, request.OrderId), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("{id:guid}/handover")]
    public async Task<IActionResult> HandoverRoute([FromRoute] Guid id, [FromBody] HandoverRouteRequest request, CancellationToken ct)
    {
        var command = new HandoverRouteCommand(id, request.Type, request.TargetShiftId, request.Assignments);
        var result = await _mediator.Send(command, ct);
        return ToResponse(result);
    }

    public record HandoverRouteRequest(HandoverType Type, Guid? TargetShiftId, IReadOnlyList<ShiftStopsAssignment>? Assignments);
    public record CreateRouteRequest(IReadOnlyList<Guid> OrderIds, DateOnly Date);
    public record AssignRouteRequest(Guid ShiftId);
    public record InsertUrgentOrderRequest(Guid OrderId);
    public record OptimizeRouteRequest(DateOnly RouteDate);
    public record PushLocationRequest(double Latitude, double Longitude);
    public record CompleteStopRequest(bool IsPartial);
    public record FailDeliveryRequest(
        double Latitude,
        double Longitude,
        FailureReason FailureReason,
        string? Notes,
        string? PhotoKey);

    private IActionResult ToResponse(Result result) =>
        result.IsSuccess ? NoContent() : BadRequest(result.Error);
 }
