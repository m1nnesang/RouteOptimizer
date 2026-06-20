namespace RouteOptimizer.Application.Orders.GetDeliveryAttempts;

public record DeliveryAttemptDto(
    Guid Id,
    Guid OrderId,
    Guid DriverId,
    DateTime AttemptedAt,
    string Outcome,
    string? FailureReason,
    string? Notes,
    string? PhotoKey,
    double Latitude,
    double Longitude);
