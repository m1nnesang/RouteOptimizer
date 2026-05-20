using FluentAssertions;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class DeliveryWindowTests
{
    [Fact]
    public void IsViolatedAt_ArrivalBeforeEnd_ReturnsFalse()
    {
        var window = DeliveryWindow.Between(
            new TimeOnly(9, 0),
            new TimeOnly(15, 0),
            WindowStrictness.Soft
        );

        var arrivalTime = new TimeOnly(10, 0);

        window.IsViolatedAt(arrivalTime).Should().BeFalse();
    }

    [Fact]
    public void IsViolatedAt_ArrivalAfterEndButWithTolerance_ReturnsFalse()
    {
        var window = DeliveryWindow.Between(
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            WindowStrictness.Soft,
            new TimeSpan(0, 5, 30));


        var arrivalTime = new TimeOnly(11, 5);

        window.IsViolatedAt(arrivalTime).Should().BeFalse();
    }

    [Fact]
    public void IsViolatedAt_ArrivalAfterEnd_ReturnsTrue()
    {
        var window = DeliveryWindow.Between(
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            WindowStrictness.Strict,
            new TimeSpan(0, 5, 30)
        );

        var arrivalTime = new TimeOnly(11, 5);

        window.IsViolatedAt(arrivalTime).Should().BeTrue();
    }
}