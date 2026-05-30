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

    // --- Between() factory ---

    [Fact]
    public void Between_Strict_HasNullTolerance()
    {
        var window = DeliveryWindow.Between(
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            WindowStrictness.Strict
        );

        window.Start.Should().Be(new TimeOnly(9, 0));
        window.End.Should().Be(new TimeOnly(17, 0));
        window.Strictness.Should().Be(WindowStrictness.Strict);
        window.Tolerance.Should().BeNull();
    }

    [Fact]
    public void Between_Soft_WithoutExplicitTolerance_UsesDefaultTenMinutes()
    {
        var window = DeliveryWindow.Between(
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            WindowStrictness.Soft
        );

        window.Tolerance.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Between_Soft_WithExplicitTolerance_UsesProvidedTolerance()
    {
        var customTolerance = TimeSpan.FromMinutes(30);
        var window = DeliveryWindow.Between(
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            WindowStrictness.Soft,
            customTolerance
        );

        window.Tolerance.Should().Be(customTolerance);
    }

    [Fact]
    public void Between_TwoIdenticalWindows_AreEqual()
    {
        var w1 = DeliveryWindow.Between(new TimeOnly(9, 0), new TimeOnly(17, 0), WindowStrictness.Strict);
        var w2 = DeliveryWindow.Between(new TimeOnly(9, 0), new TimeOnly(17, 0), WindowStrictness.Strict);

        w1.Should().Be(w2);
    }

    // --- From() factory ---

    [Fact]
    public void From_SetsStartAndNullEnd()
    {
        var window = DeliveryWindow.From(new TimeOnly(8, 0), WindowStrictness.Soft);

        window.Start.Should().Be(new TimeOnly(8, 0));
        window.End.Should().BeNull();
    }

    [Fact]
    public void From_Strict_HasNullTolerance()
    {
        var window = DeliveryWindow.From(new TimeOnly(8, 0), WindowStrictness.Strict);

        window.Tolerance.Should().BeNull();
    }

    [Fact]
    public void From_Soft_WithoutExplicitTolerance_UsesDefaultTenMinutes()
    {
        var window = DeliveryWindow.From(new TimeOnly(8, 0), WindowStrictness.Soft);

        window.Tolerance.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void From_IsViolatedAt_WithNoEndDefined_AlwaysReturnsFalse()
    {
        var window = DeliveryWindow.From(new TimeOnly(8, 0), WindowStrictness.Strict);

        window.IsViolatedAt(new TimeOnly(23, 59)).Should().BeFalse();
    }

    // --- Until() factory ---

    [Fact]
    public void Until_SetsNullStartAndEnd()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Soft);

        window.Start.Should().BeNull();
        window.End.Should().Be(new TimeOnly(18, 0));
    }

    [Fact]
    public void Until_Strict_HasNullTolerance()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Strict);

        window.Tolerance.Should().BeNull();
    }

    [Fact]
    public void Until_Soft_WithoutExplicitTolerance_UsesDefaultTenMinutes()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Soft);

        window.Tolerance.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Until_Strict_ArrivalExactlyAtEnd_IsNotViolated()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Strict);

        window.IsViolatedAt(new TimeOnly(18, 0)).Should().BeFalse();
    }

    [Fact]
    public void Until_Strict_ArrivalOneMinuteAfterEnd_IsViolated()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Strict);

        window.IsViolatedAt(new TimeOnly(18, 1)).Should().BeTrue();
    }

    [Fact]
    public void Until_Soft_ArrivalWithinTolerance_IsNotViolated()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Soft, TimeSpan.FromMinutes(10));

        window.IsViolatedAt(new TimeOnly(18, 9)).Should().BeFalse();
    }

    [Fact]
    public void Until_Soft_ArrivalBeyondTolerance_IsViolated()
    {
        var window = DeliveryWindow.Until(new TimeOnly(18, 0), WindowStrictness.Soft, TimeSpan.FromMinutes(10));

        window.IsViolatedAt(new TimeOnly(18, 11)).Should().BeTrue();
    }

    // --- AnyTime() factory ---

    [Fact]
    public void AnyTime_HasNullStartAndEnd()
    {
        var window = DeliveryWindow.AnyTime();

        window.Start.Should().BeNull();
        window.End.Should().BeNull();
    }

    [Fact]
    public void AnyTime_HasSoftStrictness()
    {
        var window = DeliveryWindow.AnyTime();

        window.Strictness.Should().Be(WindowStrictness.Soft);
    }

    [Fact]
    public void AnyTime_HasNullTolerance()
    {
        var window = DeliveryWindow.AnyTime();

        window.Tolerance.Should().BeNull();
    }

    [Fact]
    public void AnyTime_IsViolatedAt_AnyTime_ReturnsFalse()
    {
        var window = DeliveryWindow.AnyTime();

        window.IsViolatedAt(new TimeOnly(0, 0)).Should().BeFalse();
        window.IsViolatedAt(new TimeOnly(12, 0)).Should().BeFalse();
        window.IsViolatedAt(new TimeOnly(23, 59)).Should().BeFalse();
    }

    [Fact]
    public void AnyTime_TwoInstances_AreEqual()
    {
        var w1 = DeliveryWindow.AnyTime();
        var w2 = DeliveryWindow.AnyTime();

        w1.Should().Be(w2);
    }
}