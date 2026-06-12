using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Vehicles.DeleteVehicle;

public record DeleteVehicleCommand(Guid Id) : ICommand<Result>;
