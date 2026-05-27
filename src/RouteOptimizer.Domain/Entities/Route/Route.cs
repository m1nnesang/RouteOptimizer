using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Route;

namespace RouteOptimizer.Domain.Entities.Route;

public class Route : AggregateRoot<Guid>
{
    private readonly List<Stop> _stops = [];

    private Route() : base(default)
    {
    } // EF Core

    private Route(Guid id, Guid warehouseId) : base(id)
    {
        WarehouseId = warehouseId;
    }

    public Guid WarehouseId { get; }
    public Guid? AssignedShiftId { get; private set; }
    public RouteStatus Status { get; private set; } = RouteStatus.Draft;

    public IReadOnlyList<Stop> Stops => _stops.AsReadOnly();

    public IReadOnlyList<Stop> RemainingStops =>
        _stops.Where(s => s.Status is StopStatus.Pending or StopStatus.InProgress or StopStatus.Skipped)
            .ToList();

    public IReadOnlyList<Stop> CompletedStops =>
        _stops.Where(s => s.Status is StopStatus.Completed or StopStatus.PartiallyCompleted).ToList();

    public IReadOnlyList<Stop> FailedStops => _stops.Where(s => s.Status is StopStatus.Failed).ToList();

    public Stop? CurrentStop => _stops.FirstOrDefault(s => s.Status is StopStatus.InProgress)
                                ?? _stops.FirstOrDefault(s => s.Status is StopStatus.Pending);

    public static Result<Route> Create(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty)
            return Result<Route>.Failure("Stop need a warehouse");

        return Result<Route>.Success(new Route(Guid.NewGuid(), warehouseId));
    }

    public void Optimize()
    {
        if (Status is not RouteStatus.Draft)
            throw new InvalidOperationException("Route is already optimized");

        Status = RouteStatus.Optimized;
        AddDomainEvent(new RouteOptimized(Id));
    }

    public void AssignShift(Guid shiftId)
    {
        if (Status is not RouteStatus.Optimized)
            throw new InvalidOperationException("Route is not optimized");

        if (AssignedShiftId is not null)
            throw new InvalidOperationException("Shift is already assigned");

        AssignedShiftId = shiftId;
        Status = RouteStatus.Assigned;
        AddDomainEvent(new RouteAssignedToDriver(Id, shiftId));
    }

    public void Start()
    {
        if (Status is not (RouteStatus.Assigned or RouteStatus.Optimized))
            throw new InvalidOperationException("Route is not assigned/optimized");

        if (AssignedShiftId is null)
            throw new InvalidOperationException("Shift is not assigned");

        Status = RouteStatus.InProgress;
        AddDomainEvent(new RouteStarted(Id, AssignedShiftId.Value));
    }

    public void Interrupt()
    {
        if (Status is not (RouteStatus.InProgress or RouteStatus.Assigned))
            throw new InvalidOperationException("Route is not in progress");

        Status = RouteStatus.Interrupted;
        AddDomainEvent(new RouteInterrupted(Id));
    }

    public void Cancel()
    {
        if (Status is not (RouteStatus.Draft or RouteStatus.Assigned or RouteStatus.Optimized))
            throw new InvalidOperationException("Route is not in draft/assigned/optimized state");

        Status = RouteStatus.Cancelled;
    }

    public void Complete()
    {
        if (Status is not RouteStatus.InProgress)
            throw new InvalidOperationException("Route is not in progress");

        Status = RouteStatus.Completed;
        AddDomainEvent(new RouteCompleted(Id));
    }

    public void AddStop(Stop stop)
    {
        if (Status is RouteStatus.Completed or RouteStatus.Cancelled or RouteStatus.Interrupted)
            throw new InvalidOperationException("Cannot add stop to a non active route");

        _stops.Add(stop);
    }

    public void RemoveStop(Stop stop)
    {
        if (Status is RouteStatus.Completed or RouteStatus.Cancelled or RouteStatus.Interrupted)
            throw new InvalidOperationException("Cannot remove stop from a non active route");

        if (stop.Status is not StopStatus.Pending)
            throw new InvalidOperationException("Cannot remove non-pending stop");

        _stops.Remove(stop);
    }

    public void ApplyOptimizedOrders(IReadOnlyList<Guid> orderedStopIds)
    {
        var stopIds = _stops.Select(s => s.Id).ToHashSet();

        if (orderedStopIds.Count != _stops.Count ||
            !orderedStopIds.All(id => stopIds.Contains(id)))
            throw new InvalidOperationException("Ordered stop ids do not match route stops");

        if (Status is not RouteStatus.Draft)
            throw new InvalidOperationException("Route is not in draft state");

        var order = orderedStopIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        _stops.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));
    }
}
