using FluentAssertions;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.Entities;

public class StopTests
{
    private static Address ValidAddress => Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
    private static GeoCoordinate ValidLocation => GeoCoordinate.Create(52.2297, 21.0122).Value!;
    private static Guid ValidRouteId => Guid.NewGuid();

    [Fact]
    public void Create_WithValidArgs_ReturnsSuccess()
    {
        var result = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(StopStatus.Pending);
        result.Value.Sequence.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyRouteId_ReturnsFailure()
    {
        var result = Stop.Create(Guid.Empty, ValidAddress, ValidLocation, null, 0, []);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("route");
    }

    [Fact]
    public void Create_WithNegativeSequence_ReturnsFailure()
    {
        var result = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, -1, []);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sequence");
    }

    [Fact]
    public void Create_WithZeroSequence_ReturnsSuccess()
    {
        var result = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDeliveryWindow_SetsDeliveryWindow()
    {
        var window = DeliveryWindow.AnyTime();
        var result = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, window, 1, []);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeliveryWindow.Should().Be(window);
    }

    [Fact]
    public void Start_PendingStop_ChangesStatusToInProgress()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;

        stop.Start();

        stop.Status.Should().Be(StopStatus.InProgress);
    }

    [Fact]
    public void Start_AlreadyStartedStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Start();

        var act = () => stop.Start();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_InProgressStop_ChangesStatusToCompleted()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Start();

        stop.Complete();

        stop.Status.Should().Be(StopStatus.Completed);
    }

    [Fact]
    public void Complete_PendingStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;

        var act = () => stop.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PartiallyComplete_InProgressStop_ChangesStatusToPartiallyCompleted()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Start();

        stop.PartiallyComplete();

        stop.Status.Should().Be(StopStatus.PartiallyCompleted);
    }

    [Fact]
    public void PartiallyComplete_PendingStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;

        var act = () => stop.PartiallyComplete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Skip_PendingStop_ChangesStatusToSkipped()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;

        stop.Skip();

        stop.Status.Should().Be(StopStatus.Skipped);
    }

    [Fact]
    public void Skip_InProgressStop_ChangesStatusToSkipped()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Start();

        stop.Skip();

        stop.Status.Should().Be(StopStatus.Skipped);
    }

    [Fact]
    public void Skip_AlreadySkippedStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Skip();

        var act = () => stop.Skip();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Skip_CompletedStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Start();
        stop.Complete();

        var act = () => stop.Skip();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resume_SkippedStop_ChangesStatusToInProgress()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Skip();

        stop.Resume();

        stop.Status.Should().Be(StopStatus.InProgress);
    }

    [Fact]
    public void Resume_PendingStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;

        var act = () => stop.Resume();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failed_InProgressStop_ChangesStatusToFailed()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;
        stop.Start();

        stop.Failed();

        stop.Status.Should().Be(StopStatus.Failed);
    }

    [Fact]
    public void Failed_PendingStop_ThrowsInvalidOperationException()
    {
        var stop = Stop.Create(ValidRouteId, ValidAddress, ValidLocation, null, 0, []).Value!;

        var act = () => stop.Failed();

        act.Should().Throw<InvalidOperationException>();
    }
}
