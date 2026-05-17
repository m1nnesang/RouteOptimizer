using FluentAssertions;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class OrderTests
{
    private IndividualOrder CreateOrder()
    {
        var warehouse = Guid.NewGuid();
        var address = Address.Create("Main St", "Amsterdam", "1234AB", "NL").Value!;
        var location = GeoCoordinate.Create(52.3676, 4.9041).Value!;
        var weight = Weight.Create(10).Value!;
        var volume = Volume.Create(5).Value!;
        var phone = PhoneNumber.Create("+31612345678").Value!;
        var cargoType = CargoType.General;
        var notes = string.Empty;
        var customerName = "New Customer";
        var allowLeaveAtDoor = true;
        var time = DeliveryWindow.AnyTime();
    
        return IndividualOrder.Create(warehouse , address , location, weight, volume,time, phone , cargoType, notes, customerName, allowLeaveAtDoor).Value!;
    }
    
    
    
    [Fact]
    public void AssignToRoute_WhenCreated_ChangesStatusToAssigned()
    {
        var route = Guid.NewGuid();
        var order = CreateOrder();
        
        order.AssignToRoute(route);
        
        order.Status.Should().Be(OrderStatus.AssignedToRoute);
        order.AssignedRouteId.Should().Be(route);
    }

    [Fact]
    public void AssignToRoute_WhenAlreadyAssigned_ThrowsException()
    {
        var order = CreateOrder();
        order.AssignToRoute(Guid.NewGuid());

        var act = () => order.AssignToRoute(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsDelivered_WhenInTransit_ChangesStatusToDelivered()
    {
        var order = CreateOrder();
        
        order.AssignToRoute(Guid.NewGuid());
        order.MarkAsInTransit();
        order.MarkAsDelivered();
        
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void MarkAsDelivered_WhenNotInTransit_ThrowsException()
    {
        var order = CreateOrder();
        order.AssignToRoute(Guid.NewGuid());
        
        var act = () => order.MarkAsDelivered();
        
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsFailed_WhenInTransit_ResetsAssignedRouteId()
    {
        var order = CreateOrder();
        order.AssignToRoute(Guid.NewGuid());
        order.MarkAsInTransit();
        order.MarkAsFailed();
        
        order.AssignedRouteId.Should().Be(null);
    }

    [Fact]
    public void Cancel_WhenDelivered_ThrowsException()
    {
        var order = CreateOrder();
        order.AssignToRoute(Guid.NewGuid());
        order.MarkAsInTransit();
        order.MarkAsDelivered();
        
        var act = () => order.MarkAsFailed();
        
        act.Should().Throw<InvalidOperationException>();
    }
}