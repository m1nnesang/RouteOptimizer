using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.Entities;

public class DriverShift : AggregateRoot<Guid>
{
    private DriverShift(Guid id, Guid driverId, Guid vehicleId, Guid warehouseId, DateOnly shiftDate,
        DateTime? startedAt, DateTime? endedAt) : base(id)
    {
        (DriverId, VehicleId, WarehouseId, ShiftDate, StartedAt, EndedAt) =
            (driverId, vehicleId, warehouseId, shiftDate, startedAt, endedAt);
    }

    public Guid DriverId { get; }
    public Guid VehicleId { get; }
    public Guid WarehouseId { get; }

    public DateOnly ShiftDate { get; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    public void Start()
    {
        if (StartedAt is not null)
            throw new InvalidOperationException("Shift has already started");

        StartedAt = DateTime.UtcNow;
    }

    public void End()
    {
        if (StartedAt is null)
            throw new InvalidOperationException("Shift has not started");

        if (EndedAt is not null)
            throw new InvalidOperationException("Shift has already ended");

        EndedAt = DateTime.UtcNow;
    }

    public static Result<DriverShift> Create(Guid driverId, Guid vehicleId, Guid warehouseId,
        DateOnly shiftDate)
    {
        if (shiftDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<DriverShift>.Failure("Shift cannot be in the past");

        if (driverId == Guid.Empty || vehicleId == Guid.Empty || warehouseId == Guid.Empty)
            return Result<DriverShift>.Failure("Please fill all fields");

        return Result<DriverShift>.Success(new DriverShift(Guid.NewGuid(), driverId, vehicleId, warehouseId, shiftDate,
            null, null));
    }
}