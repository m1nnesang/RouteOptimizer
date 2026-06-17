using System.Net;
using FluentAssertions;
using Moq;
using RouteOptimizer.Domain.ValueObjects;
using RouteOptimizer.Infrastructure.Services;

namespace RouteOptimizer.Infrastructure.Tests.Services;

public class NominatimGeocodingServiceTests
{
    private static NominatimGeocodingService BuildSut(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Nominatim")).Returns(client);
        return new NominatimGeocodingService(factory.Object);
    }

    private static Address TestAddress() =>
        Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;

    [Fact]
    public async Task GeocodeAsync_AddressFound_ReturnsCoordinate()
    {
        var json = """[{"lat":"52.2297","lon":"21.0122"}]""";
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var result = await sut.GeocodeAsync(TestAddress(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Latitude.Should().BeApproximately(52.2297, 1e-4);
        result.Value!.Longitude.Should().BeApproximately(21.0122, 1e-4);
    }

    [Fact]
    public async Task GeocodeAsync_EmptyResults_ReturnsFailure()
    {
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        });

        var result = await sut.GeocodeAsync(TestAddress(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GeocodeAsync_HttpError_ThrowsException()
    {
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var act = () => sut.GeocodeAsync(TestAddress(), default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(response);
    }
}
