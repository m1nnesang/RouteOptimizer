using System.Net.Http.Json;
using System.Text.Json.Serialization;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Infrastructure.Services;

public sealed class OsrmDistanceMatrixProvider : IDistanceMatrixProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OsrmDistanceMatrixProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<DistanceMatrix> GetMatrixAsync((double Lat, double Lon) warehouse,
        IReadOnlyList<(double Lat, double Lon)> input,
        CancellationToken ct = default)
    {
        var coords = new List<string>();
        coords.Add($"{warehouse.Lon},{warehouse.Lat}");

        foreach (var stopCoords in input )
        {
            coords.Add($"{stopCoords.Lon},{stopCoords.Lat}");
        }

        var coordsString = string.Join(";", coords);

       var client = _httpClientFactory.CreateClient("Osrm");

       var url = $"/table/v1/driving/{coordsString}?sources=all&destinations=all&annotations=duration";

       var osrmResponse = await client.GetFromJsonAsync<OsrmTableResponse>(url, ct);

       if (osrmResponse?.Durations is null)
           throw new InvalidOperationException("Osrm returned null durations");

       var size = osrmResponse.Durations.Length;
       double[,] matrix = new double[size, size];

       for (int i = 0; i < osrmResponse.Durations.Length; i++)
       {
           for (int j = 0; j < osrmResponse.Durations[i].Length; j++)
           {
               matrix[i, j] = osrmResponse.Durations[i][j];
           }
       }

       return new DistanceMatrix(matrix);
    }

    private sealed record OsrmTableResponse(
        [property: JsonPropertyName("durations")] double[][] Durations
    );

}
