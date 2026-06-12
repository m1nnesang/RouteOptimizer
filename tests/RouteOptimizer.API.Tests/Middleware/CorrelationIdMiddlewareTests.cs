using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RouteOptimizer.API.Middleware;

namespace RouteOptimizer.API.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private const string Header = "X-Correlation-ID";

    private static async Task<HttpContext> Run(string? incoming)
    {
        var context = new DefaultHttpContext();
        if (incoming is not null)
            context.Request.Headers[Header] = incoming;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task GeneratesGuid_WhenHeaderMissing()
    {
        var context = await Run(null);

        var header = context.Response.Headers[Header].ToString();
        Guid.TryParse(header, out _).Should().BeTrue();
    }

    [Fact]
    public async Task EchoesIncomingValue_WhenValid()
    {
        var id = Guid.NewGuid().ToString();
        var context = await Run(id);

        context.Response.Headers[Header].ToString().Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeneratesNewGuid_WhenIncomingInvalid(string bad)
    {
        var context = await Run(bad);

        var header = context.Response.Headers[Header].ToString();
        header.Should().NotBe(bad);
        Guid.TryParse(header, out _).Should().BeTrue();
    }

    [Fact]
    public async Task GeneratesNewGuid_WhenIncomingTooLong()
    {
        var tooLong = new string('a', 65);
        var context = await Run(tooLong);

        var header = context.Response.Headers[Header].ToString();
        header.Should().NotBe(tooLong);
        Guid.TryParse(header, out _).Should().BeTrue();
    }

    [Fact]
    public async Task SetsResponseHeader_Always()
    {
        var context = await Run(null);

        context.Response.Headers.ContainsKey(Header).Should().BeTrue();
    }
}
