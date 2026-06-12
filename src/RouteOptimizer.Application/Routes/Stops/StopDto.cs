namespace RouteOptimizer.Application.Routes.Stops;

public record StopDto(Guid Id, int Sequence, string City, string Street, double Latitude, double Longitude, string Status, IReadOnlyList<Guid> OrderIds);
