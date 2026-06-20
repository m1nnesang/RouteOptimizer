namespace RouteOptimizer.Application.Routes.Stops;

public record StopDto(Guid Id, int Sequence, string City, string Street, double Latitude, double Longitude, string Status, IReadOnlyList<Guid> OrderIds, IReadOnlyList<StopOrderDto> Orders);

public record StopOrderDto(Guid OrderId, string Recipient, string? Apartment, string Type, string Phone, string Status);
