using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Vehicles.CreateVehicle;
using RouteOptimizer.Application.Vehicles.DeleteVehicle;
using RouteOptimizer.Application.Vehicles.GetVehicles;
using RouteOptimizer.Application.Vehicles.UpdateVehicle;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehiclesController(IMediator mediator) => _mediator = mediator;

    private IActionResult ToResponse(Result result) =>
        result.IsSuccess ? NoContent() : BadRequest(result.Error);

    [Authorize(Roles = "Dispatcher,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetByWarehouse([FromQuery] Guid warehouseId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVehiclesQuery(warehouseId), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateVehicleCommand(id, request.Type, request.MaxWeightKg,
            request.MaxVolumeM3, request.LicenseCategory, request.AllowedCargoTypes), ct);
        return ToResponse(result);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteVehicleCommand(id), ct);
        return ToResponse(result);
    }

    public record UpdateVehicleRequest(
        string Type,
        decimal MaxWeightKg,
        decimal MaxVolumeM3,
        LicenseCategory LicenseCategory,
        IReadOnlyList<CargoType> AllowedCargoTypes);
}
