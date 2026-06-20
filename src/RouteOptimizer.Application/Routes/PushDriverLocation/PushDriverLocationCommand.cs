using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Routes.PushDriverLocation;

public record PushDriverLocationCommand(Guid RouteId, double Latitude, double Longitude) : ICommand<Result>;
