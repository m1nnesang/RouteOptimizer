using FluentAssertions;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class RouteTests
{
    private static Guid ValidWarehouseId => Guid.NewGuid();

    private static Stop CreateStop(Guid routeId)
    {
        var address = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        return Stop.Create(routeId, address, location, null, 0, []).Value!;
    }

    // --- Create() ---

    [Fact]
    public void Create_WithValidWarehouseId_ReturnsSuccess()
    {
        var warehouseId = ValidWarehouseId;
        var result = Route.Create(warehouseId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(RouteStatus.Draft);
        result.Value.WarehouseId.Should().Be(warehouseId);
    }

    [Fact]
    public void Create_WithEmptyWarehouseId_ReturnsFailure()
    {
        var result = Route.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("warehouse");
    }

    [Fact]
    public void Create_NewRoute_HasDraftStatusAndNoStops()
    {
        var route = Route.Create(ValidWarehouseId).Value!;

        route.Status.Should().Be(RouteStatus.Draft);
        route.AssignedShiftId.Should().BeNull();
        route.Stops.Should().BeEmpty();
    }

    // --- Optimize() ---

    [Fact]
    public void Optimize_DraftRoute_ChangesStatusToOptimized()
    {
        var route = Route.Create(ValidWarehouseId).Value!;

        route.Optimize();

        route.Status.Should().Be(RouteStatus.Optimized);
    }

    [Fact]
    public void Optimize_AlreadyOptimizedRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();

        var act = () => route.Optimize();

        act.Should().Throw<InvalidOperationException>();
    }

    // --- AssignShift() ---

    [Fact]
    public void AssignShift_OptimizedRoute_ChangesStatusToAssigned()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        var shiftId = Guid.NewGuid();

        route.AssignShift(shiftId);

        route.Status.Should().Be(RouteStatus.Assigned);
        route.AssignedShiftId.Should().Be(shiftId);
    }

    [Fact]
    public void AssignShift_DraftRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;

        var act = () => route.AssignShift(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssignShift_AlreadyAssignedRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());

        var act = () => route.AssignShift(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Start() ---

    [Fact]
    public void Start_AssignedRoute_ChangesStatusToInProgress()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());

        route.Start();

        route.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public void Start_DraftRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;

        var act = () => route.Start();

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Complete() ---

    [Fact]
    public void Complete_InProgressRoute_ChangesStatusToCompleted()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();

        route.Complete();

        route.Status.Should().Be(RouteStatus.Completed);
    }

    [Fact]
    public void Complete_DraftRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;

        var act = () => route.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Interrupt() ---

    [Fact]
    public void Interrupt_InProgressRoute_ChangesStatusToInterrupted()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();

        route.Interrupt();

        route.Status.Should().Be(RouteStatus.Interrupted);
    }

    [Fact]
    public void Interrupt_AssignedRoute_ChangesStatusToInterrupted()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());

        route.Interrupt();

        route.Status.Should().Be(RouteStatus.Interrupted);
    }

    [Fact]
    public void Interrupt_CompletedRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        route.Complete();

        var act = () => route.Interrupt();

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Cancel() ---

    [Fact]
    public void Cancel_DraftRoute_ChangesStatusToCancelled()
    {
        var route = Route.Create(ValidWarehouseId).Value!;

        route.Cancel();

        route.Status.Should().Be(RouteStatus.Cancelled);
    }

    [Fact]
    public void Cancel_OptimizedRoute_ChangesStatusToCancelled()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();

        route.Cancel();

        route.Status.Should().Be(RouteStatus.Cancelled);
    }

    [Fact]
    public void Cancel_InProgressRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();

        var act = () => route.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    // --- AddStop() / RemoveStop() ---

    [Fact]
    public void AddStop_DraftRoute_AddsStopToCollection()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop = CreateStop(route.Id);

        route.AddStop(stop);

        route.Stops.Should().ContainSingle();
    }

    [Fact]
    public void AddStop_CancelledRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        route.Cancel();
        var stop = CreateStop(route.Id);

        var act = () => route.AddStop(stop);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveStop_PendingStop_RemovesFromCollection()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop = CreateStop(route.Id);
        route.AddStop(stop);

        route.RemoveStop(stop);

        route.Stops.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStop_CancelledRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop = CreateStop(route.Id);
        route.AddStop(stop);
        route.Cancel();

        var act = () => route.RemoveStop(stop);

        act.Should().Throw<InvalidOperationException>();
    }

    // --- ApplyOptimizedOrders() ---

    [Fact]
    public void ApplyOptimizedOrders_ValidOrder_ReordersStops()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop1 = CreateStop(route.Id);
        var stop2 = CreateStop(route.Id);
        route.AddStop(stop1);
        route.AddStop(stop2);

        var orderedIds = new List<Guid> { stop2.Id, stop1.Id };
        route.ApplyOptimizedOrders(orderedIds);

        route.Stops[0].Id.Should().Be(stop2.Id);
        route.Stops[1].Id.Should().Be(stop1.Id);
    }

    [Fact]
    public void ApplyOptimizedOrders_MismatchedIds_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop = CreateStop(route.Id);
        route.AddStop(stop);

        var act = () => route.ApplyOptimizedOrders([Guid.NewGuid()]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApplyOptimizedOrders_NonDraftRoute_ThrowsInvalidOperationException()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop = CreateStop(route.Id);
        route.AddStop(stop);
        route.Optimize();

        var act = () => route.ApplyOptimizedOrders([stop.Id]);

        act.Should().Throw<InvalidOperationException>();
    }
}
