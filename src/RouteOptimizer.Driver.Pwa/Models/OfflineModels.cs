namespace RouteOptimizer.Driver.Pwa.Models;

public enum OutboxKind
{
    DeliverOrder,
    FailOrder,
    SkipStop,
    ResumeStop,
    StartRoute,
    CompleteRoute,
    Location
}

public sealed record OutboxItem(
    Guid Id,
    OutboxKind Kind,
    Guid RouteId,
    Guid? StopId,
    Guid? OrderId,
    double? Latitude,
    double? Longitude,
    string? FailureReason,
    string? Notes,
    string? PhotoKey,
    DateTime CreatedAt);
