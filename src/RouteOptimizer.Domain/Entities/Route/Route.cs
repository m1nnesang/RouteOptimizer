using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.Events.Route;
using RouteOptimizer.Domain.ValueObjects;

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

    public Result InsertUrgentOrder(Order order)
    {
        if (Status != RouteStatus.InProgress)
            return Result.Failure("Route is not in progress");

        if (order.WarehouseId != WarehouseId)
            return Result.Failure("Order belongs to a different warehouse");

        if (order.Status != OrderStatus.Created)
            return Result.Failure("Only created orders can be inserted");

        var sequenceNumber = _stops.Count > 0 ? _stops.Max(s => s.Sequence) + 1 : 0;

        var address = Address.Create(order.Address.Street, order.Address.City, order.Address.PostalCode, order.Address.Country).Value!;
        var location = GeoCoordinate.Create(order.Location.Latitude, order.Location.Longitude).Value!;
        var deliveryWindow = CopyDeliveryWindow(order.DeliveryWindow);

        var stop = Stop.Create(Id, address, location, deliveryWindow, sequenceNumber, [order.Id]);
        if (stop.IsFailure) return Result.Failure(stop.Error ?? "Failed to create stop");

        _stops.Add(stop.Value!);
        order.AssignToRoute(Id);

        return Result.Success();
    }

    public void RemoveStop(Stop stop)
    {
        if (Status is RouteStatus.Completed or RouteStatus.Cancelled or RouteStatus.Interrupted)
            throw new InvalidOperationException("Cannot remove stop from a non active route");

        if (stop.Status is not StopStatus.Pending)
            throw new InvalidOperationException("Cannot remove non-pending stop");

        _stops.Remove(stop);
    }

    public Result ReorderRemainingStops(IReadOnlyList<Guid> orderedStopIds)
    {
        if (Status != RouteStatus.InProgress)
            return Result.Failure("Route is not in progress");

        var remainingIds = _stops
            .Where(s => s.Status != StopStatus.Completed && s.Status != StopStatus.PartiallyCompleted)
            .Select(s => s.Id)
            .ToHashSet();

        if (orderedStopIds.Count != remainingIds.Count || !orderedStopIds.All(id => remainingIds.Contains(id)))
            return Result.Failure("Ordered stop ids do not match remaining stops");

        var order = orderedStopIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        _stops.Sort((a, b) =>
        {
            var aIsCompleted = !remainingIds.Contains(a.Id);
            var bIsCompleted = !remainingIds.Contains(b.Id);
            if (aIsCompleted && bIsCompleted) return a.Sequence.CompareTo(b.Sequence);
            if (aIsCompleted) return -1;
            if (bIsCompleted) return 1;
            return order[a.Id].CompareTo(order[b.Id]);
        });

        for (var i = 0; i < _stops.Count; i++)
            _stops[i].UpdateSequence(i);

        return Result.Success();
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

        for (var i = 0; i < _stops.Count; i++)
            _stops[i].UpdateSequence(i);
    }

    public Result RemoveCancelledOrder(Guid orderId)
    {
        if (Status is RouteStatus.Completed or RouteStatus.Cancelled or RouteStatus.Interrupted)
            return Result.Failure("Cannot modify a finished route");

        var stop = _stops.FirstOrDefault(s => s.Orders.Contains(orderId));
        if (stop is null)
            return Result.Success();

        if (stop.Status is not StopStatus.Pending)
            return Result.Failure("Cannot remove order from a stop that is already in progress or finished");

        stop.RemoveOrder(orderId);

        if (stop.Orders.Count == 0)
        {
            _stops.Remove(stop);

            for (var i = 0; i < _stops.Count; i++)
                _stops[i].UpdateSequence(i);
        }

        return Result.Success();
    }

    public Stop? StartFirstPendingStop()
    {
        var first = _stops.Where(s => s.Status == StopStatus.Pending)
                          .OrderBy(s => s.Sequence)
                          .FirstOrDefault();
        first?.Start();
        return first;
    }

    public Stop? AdvanceToNextStop()
    {
        var next = _stops.Where(s => s.Status == StopStatus.Pending)
                         .OrderBy(s => s.Sequence)
                         .FirstOrDefault();
        next?.Start();
        return next;
    }

    private static DeliveryWindow? CopyDeliveryWindow(DeliveryWindow? window) =>
        window switch
        {
            null => null,
            { Start: not null, End: not null } w => DeliveryWindow.Between(w.Start.Value, w.End.Value, w.Strictness, w.Tolerance),
            { Start: not null } w => DeliveryWindow.From(w.Start.Value, w.Strictness, w.Tolerance),
            { End: not null } w => DeliveryWindow.Until(w.End.Value, w.Strictness, w.Tolerance),
            _ => DeliveryWindow.AnyTime()
        };
}
