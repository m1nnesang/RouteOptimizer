using FluentAssertions;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.ValueObjects;

public class GeoCoordinateTests
{
    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Create_WithInvalidCoordinates_ReturnsFailure(double lat, double lng)
    {
        var result = GeoCoordinate.Create(lat, lng);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DistanceTo_WarsawToKrakow_Returns252Km()
    {
        var warsaw = GeoCoordinate.Create(52.2322, 21.0083).Value!;
        var krakow = GeoCoordinate.Create(50.0614, 19.9383).Value!;

        var distance = warsaw.DistanceTo(krakow);
        distance.Should().BeApproximately(252, 5);
    }

    [Fact]
    public void Create_WithValidCoordinates_ReturnsSuccess()
    {
        var result = GeoCoordinate.Create(52.2322, 21.0083);
        result.IsSuccess.Should().BeTrue();
    }
}
