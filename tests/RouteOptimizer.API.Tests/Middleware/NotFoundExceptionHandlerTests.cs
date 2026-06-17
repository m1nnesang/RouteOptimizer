using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RouteOptimizer.API.Middleware;
using RouteOptimizer.Application.Exceptions;

namespace RouteOptimizer.API.Tests.Middleware;

public class NotFoundExceptionHandlerTests
{
    private readonly NotFoundExceptionHandler _handler = new();

    [Fact]
    public async Task TryHandleAsync_NonNotFoundException_ReturnsFalse()
    {
        var context = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidOperationException(), default);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_SetsNotFoundStatus()
    {
        var context = CreateContextWithBody();

        var handled = await _handler.TryHandleAsync(context, new NotFoundException("Order not found"), default);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task TryHandleAsync_WritesExceptionMessage()
    {
        var context = CreateContextWithBody();

        await _handler.TryHandleAsync(context, new NotFoundException("Order not found"), default);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        document.RootElement.GetProperty("error").GetString().Should().Be("Order not found");
    }

    private static DefaultHttpContext CreateContextWithBody()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
