using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities;

public class DeliveryAttempt : Entity<Guid>
{
    private DeliveryAttempt() : base(default)
    {
        AttemptLocation = null!;
    }

    private DeliveryAttempt(Guid id, Guid orderId, Guid driverId, Guid routeId, DateTime attemptedAt,
        GeoCoordinate attemptLocation, DeliveryOutcome outcome, FailureReason? failureReason, string? notes,
        string? photoKey) : base(id)
    {
        (OrderId, DriverId, RouteId, AttemptedAt, AttemptLocation, Outcome, FailureReason, Notes, PhotoKey) = (orderId,
            driverId, routeId, attemptedAt, attemptLocation, outcome, failureReason, notes, photoKey);
    }

    public Guid OrderId { get; }
    public Guid DriverId { get; }
    public Guid RouteId { get; }
    public DateTime AttemptedAt { get; }
    public GeoCoordinate AttemptLocation { get; }
    public DeliveryOutcome Outcome { get; }
    public FailureReason? FailureReason { get; }
    public string? Notes { get; }

    public string? PhotoKey { get; }

    public static Result<DeliveryAttempt> Create(Guid orderId, Guid driverId, Guid routeId,
        GeoCoordinate attemptLocation, DeliveryOutcome outcome, FailureReason? failureReason, string? notes,
        string? photoKey = null)
    {
        if (failureReason is Enums.FailureReason.Other && string.IsNullOrEmpty(notes))
            return Result<DeliveryAttempt>.Failure("Notes need to be filled for other failure reason");

        var attemptedAt = DateTime.UtcNow;

        return Result<DeliveryAttempt>.Success(new DeliveryAttempt(Guid.NewGuid(), orderId, driverId, routeId,
            attemptedAt, attemptLocation, outcome, failureReason, notes, photoKey));
    }
}