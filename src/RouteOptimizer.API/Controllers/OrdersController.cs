using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Orders.CancelOrder;
using RouteOptimizer.Application.Orders.CreateBusinessOrder;
using RouteOptimizer.Application.Orders.CreateIndividualOrder;
using RouteOptimizer.Application.Orders.GetOrderById;
using RouteOptimizer.Application.Orders.GetOrders;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    private IActionResult ToResponse(Result result) =>
        result.IsSuccess ? NoContent() : BadRequest(result.Error);

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] OrderStatus? status, [FromQuery] DateOnly? date,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status, date, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("business")]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessOrderCommand
        command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpPost("individual")]
    public async Task<IActionResult> CreateIndividual([FromBody] CreateIndividualOrderCommand
        command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Dispatcher")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

}
