using FluentAssertions;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.ValueObjects;

public class VolumeTests
{
    [Fact]
    public void Create_WithPositiveValue_ReturnsSuccess()
    {
        var result = Volume.Create(10m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(10m);
    }

    [Fact]
    public void Create_WithZero_ReturnsSuccess()
    {
        var result = Volume.Create(0m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeValue_ReturnsFailure()
    {
        var result = Volume.Create(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Volume must be greater than 0");
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    [InlineData(-999.99)]
    public void Create_WithVariousNegativeValues_ReturnsFailure(double raw)
    {
        var result = Volume.Create((decimal)raw);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_TwoVolumesWithSameValue_AreEqual()
    {
        var v1 = Volume.Create(5m).Value!;
        var v2 = Volume.Create(5m).Value!;

        v1.Should().Be(v2);
    }

    [Fact]
    public void Create_TwoVolumesWithDifferentValues_AreNotEqual()
    {
        var v1 = Volume.Create(5m).Value!;
        var v2 = Volume.Create(10m).Value!;

        v1.Should().NotBe(v2);
    }
}
