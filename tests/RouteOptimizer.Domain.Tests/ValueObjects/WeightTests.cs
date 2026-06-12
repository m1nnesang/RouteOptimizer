using FluentAssertions;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.ValueObjects;

public class WeightTests
{
    [Fact]
    public void Create_WithPositiveValue_ReturnsSuccess()
    {
        var result = Weight.Create(25m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(25m);
    }

    [Fact]
    public void Create_WithZero_ReturnsSuccess()
    {
        var result = Weight.Create(0m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeValue_ReturnsFailure()
    {
        var result = Weight.Create(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Weight must be greater than 0");
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-50)]
    [InlineData(-1000)]
    public void Create_WithVariousNegativeValues_ReturnsFailure(double raw)
    {
        var result = Weight.Create((decimal)raw);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_TwoWeightsWithSameValue_AreEqual()
    {
        var w1 = Weight.Create(12.5m).Value!;
        var w2 = Weight.Create(12.5m).Value!;

        w1.Should().Be(w2);
    }

    [Fact]
    public void Create_TwoWeightsWithDifferentValues_AreNotEqual()
    {
        var w1 = Weight.Create(10m).Value!;
        var w2 = Weight.Create(20m).Value!;

        w1.Should().NotBe(w2);
    }
}
