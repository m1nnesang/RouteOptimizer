using System.Net;
using FluentAssertions;
using Moq;
using RouteOptimizer.Infrastructure.Services;

namespace RouteOptimizer.Infrastructure.Tests.Services;

public class OsrmDistanceMatrixProviderTests
{
    private static OsrmDistanceMatrixProvider BuildSut(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://osrm") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Osrm")).Returns(client);
        return new OsrmDistanceMatrixProvider(factory.Object);
    }

    [Fact]
    public async Task GetMatrixAsync_ValidResponse_ReturnsParsedMatrix()
    {
        var json = """{"durations":[[0,120],[130,0]]}""";
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var result = await sut.GetMatrixAsync(
            (52.52, 13.40),
            new[] { (52.53, 13.41) });

        result.Should().NotBeNull();
        result.GetDuration(0, 1).Should().Be(120);
        result.GetDuration(1, 0).Should().Be(130);
    }

    [Fact]
    public async Task GetMatrixAsync_NullDurations_ThrowsInvalidOperation()
    {
        var json = """{"durations":null}""";
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var act = () => sut.GetMatrixAsync((52.52, 13.40), new[] { (52.53, 13.41) });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*null durations*");
    }

    [Fact]
    public async Task GetMatrixAsync_MultipleStops_MatrixSizeIsStopsPlusOne()
    {
        var json = """{"durations":[[0,100,200],[110,0,150],[210,160,0]]}""";
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var result = await sut.GetMatrixAsync(
            (52.52, 13.40),
            new[] { (52.53, 13.41), (52.54, 13.42) });

        result.GetDuration(0, 0).Should().Be(0);
        result.GetDuration(0, 2).Should().Be(200);
        result.GetDuration(2, 1).Should().Be(160);
    }

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(response);
    }
}
