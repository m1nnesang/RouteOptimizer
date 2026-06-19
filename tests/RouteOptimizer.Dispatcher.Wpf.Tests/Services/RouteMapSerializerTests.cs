using System.Text.Json;
using FluentAssertions;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.Services;

public class RouteMapSerializerTests
{
    [Fact]
    public void Serialize_OrdersBySequence()
    {
        var stops = new[]
        {
            new RouteStop { Sequence = 2, Latitude = 52.1, Longitude = 5.1 },
            new RouteStop { Sequence = 1, Latitude = 52.2, Longitude = 5.2 },
        };

        var markers = Parse(RouteMapSerializer.Serialize(stops));

        markers.Select(m => m.GetProperty("seq").GetInt32()).Should().ContainInOrder(2, 3);
    }

    [Fact]
    public void Serialize_SkipsStopsWithoutCoordinates()
    {
        var stops = new[]
        {
            new RouteStop { Sequence = 1, Latitude = 0, Longitude = 0 },
            new RouteStop { Sequence = 2, Latitude = 52.2, Longitude = 5.2 },
        };

        var markers = Parse(RouteMapSerializer.Serialize(stops));

        markers.Should().HaveCount(1);
        markers[0].GetProperty("seq").GetInt32().Should().Be(3);
    }

    [Fact]
    public void Serialize_BuildsLabelFromStreetAndCity()
    {
        var stops = new[]
        {
            new RouteStop { Sequence = 1, Latitude = 52.2, Longitude = 5.2, Street = "Damrak", City = "Amsterdam" },
        };

        var markers = Parse(RouteMapSerializer.Serialize(stops));

        markers[0].GetProperty("label").GetString().Should().Be("Damrak, Amsterdam");
    }

    [Fact]
    public void Serialize_FallsBackToStopLabel_WhenAddressMissing()
    {
        var stops = new[]
        {
            new RouteStop { Sequence = 3, Latitude = 52.2, Longitude = 5.2 },
        };

        var markers = Parse(RouteMapSerializer.Serialize(stops));

        markers[0].GetProperty("label").GetString().Should().Be("Stop 4");
    }

    [Fact]
    public void Serialize_EmptyInput_ReturnsEmptyArray()
    {
        var json = RouteMapSerializer.Serialize(Array.Empty<RouteStop>());

        Parse(json).Should().BeEmpty();
    }

    private static List<JsonElement> Parse(string json) =>
        JsonSerializer.Deserialize<List<JsonElement>>(json)!;
}
