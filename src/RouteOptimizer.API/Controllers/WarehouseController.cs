using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Warehouses.CreateWarehouse;
using RouteOptimizer.Application.Warehouses.DeleteWarehouse;
using RouteOptimizer.Application.Warehouses.GetWarehouses;
using RouteOptimizer.Application.Warehouses.UpdateWarehouse;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehousesController(IMediator mediator) => _mediator = mediator;

    private IActionResult ToResponse(Result result) =>
        result.IsSuccess ? NoContent() : BadRequest(result.Error);

    [Authorize(Roles = "Dispatcher,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetWarehouses(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWarehousesQuery(), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateWarehouseCommand(id, request.Name, request.Street, request.City, request.PostalCode, request.Country), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteWarehouseCommand(id), ct);
        return ToResponse(result);
    }

    public record UpdateWarehouseRequest(string Name, string Street, string City, string PostalCode, string Country);
}
