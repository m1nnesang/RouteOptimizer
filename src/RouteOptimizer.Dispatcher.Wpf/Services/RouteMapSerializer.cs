using System.Text.Json;
using RouteOptimizer.Dispatcher.Wpf.Models;

namespace RouteOptimizer.Dispatcher.Wpf.Services;

public static class RouteMapSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(IEnumerable<RouteStop> stops)
    {
        var markers = stops
            .Where(HasCoordinates)
            .OrderBy(s => s.Sequence)
            .Select(s => new
            {
                lat = s.Latitude,
                lng = s.Longitude,
                seq = s.Sequence,
                status = s.Status,
                label = BuildLabel(s),
            });

        return JsonSerializer.Serialize(markers, Options);
    }

    private static bool HasCoordinates(RouteStop stop) =>
        stop.Latitude != 0 || stop.Longitude != 0;

    private static string BuildLabel(RouteStop stop)
    {
        var address = string.Join(", ",
            new[] { stop.Street, stop.City }.Where(p => !string.IsNullOrWhiteSpace(p)));
        return address.Length == 0 ? $"Stop {stop.Sequence}" : address;
    }
}
