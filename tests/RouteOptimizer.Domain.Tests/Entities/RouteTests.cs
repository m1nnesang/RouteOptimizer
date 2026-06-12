using FluentAssertions;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.Entities;

public class RouteTests
{
    private static Guid ValidWarehouseId => Guid.NewGuid();

    private static Stop CreateStop(Guid routeId)
    {
        var address = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        return Stop.Create(routeId, address, location, null, 0, []).Value!;
    }

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

    [Fact]
    public void InsertUrgentOrder_InProgressRoute_SameWarehouse_ReturnsSuccess()
    {
        var warehouseId = ValidWarehouseId;
        var route = CreateInProgressRoute(warehouseId);
        var order = CreateBusinessOrder(warehouseId);

        var result = route.InsertUrgentOrder(order);

        result.IsSuccess.Should().BeTrue();
        route.Stops.Should().HaveCount(1);
        order.AssignedRouteId.Should().Be(route.Id);
    }

    [Fact]
    public void InsertUrgentOrder_NotInProgress_ReturnsFailure()
    {
        var warehouseId = ValidWarehouseId;
        var route = Route.Create(warehouseId).Value!;
        var order = CreateBusinessOrder(warehouseId);

        var result = route.InsertUrgentOrder(order);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void InsertUrgentOrder_DifferentWarehouse_ReturnsFailure()
    {
        var route = CreateInProgressRoute(ValidWarehouseId);
        var order = CreateBusinessOrder(Guid.NewGuid());

        var result = route.InsertUrgentOrder(order);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void InsertUrgentOrder_OrderAlreadyAssigned_ReturnsFailure()
    {
        var warehouseId = ValidWarehouseId;
        var route = CreateInProgressRoute(warehouseId);
        var order = CreateBusinessOrder(warehouseId);
        order.AssignToRoute(Guid.NewGuid());

        var result = route.InsertUrgentOrder(order);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReorderRemainingStops_InProgress_ValidIds_ReturnsSuccessAndReorders()
    {
        var route = CreateInProgressRoute(ValidWarehouseId);
        var stop1 = CreateStop(route.Id);
        var stop2 = CreateStop(route.Id);
        route.AddStop(stop1);
        route.AddStop(stop2);

        var result = route.ReorderRemainingStops([stop2.Id, stop1.Id]);

        result.IsSuccess.Should().BeTrue();
        route.Stops[0].Id.Should().Be(stop2.Id);
        route.Stops[1].Id.Should().Be(stop1.Id);
    }

    [Fact]
    public void ReorderRemainingStops_NotInProgress_ReturnsFailure()
    {
        var route = Route.Create(ValidWarehouseId).Value!;
        var stop = CreateStop(route.Id);
        route.AddStop(stop);

        var result = route.ReorderRemainingStops([stop.Id]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReorderRemainingStops_MismatchedIds_ReturnsFailure()
    {
        var route = CreateInProgressRoute(ValidWarehouseId);
        var stop = CreateStop(route.Id);
        route.AddStop(stop);

        var result = route.ReorderRemainingStops([Guid.NewGuid()]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemainingStops_AfterCompleting_ReturnsOnlyPending()
    {
        var route = CreateInProgressRoute(ValidWarehouseId);
        var stop1 = CreateStop(route.Id);
        var stop2 = CreateStop(route.Id);
        route.AddStop(stop1);
        route.AddStop(stop2);
        stop1.Start();
        stop1.Complete();

        route.RemainingStops.Should().ContainSingle().Which.Id.Should().Be(stop2.Id);
        route.CompletedStops.Should().ContainSingle().Which.Id.Should().Be(stop1.Id);
    }

    private static Route CreateInProgressRoute(Guid warehouseId)
    {
        var route = Route.Create(warehouseId).Value!;
        route.Optimize();
        route.AssignShift(Guid.NewGuid());
        route.Start();
        return route;
    }

    private static BusinessOrder CreateBusinessOrder(Guid warehouseId)
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        var weight = Weight.Create(10m).Value!;
        var volume = Volume.Create(1m).Value!;
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return BusinessOrder.Create(warehouseId, address, location, weight, volume,
            DeliveryWindow.AnyTime(), phone, CargoType.General, null, "Firma", "Jan").Value!;
    }
}
