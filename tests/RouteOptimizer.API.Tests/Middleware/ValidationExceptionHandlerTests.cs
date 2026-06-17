using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using RouteOptimizer.API.Middleware;

namespace RouteOptimizer.API.Tests.Middleware;

public class ValidationExceptionHandlerTests
{
    private readonly ValidationExceptionHandler _handler = new();

    [Fact]
    public async Task TryHandleAsync_NonValidationException_ReturnsFalse()
    {
        var context = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidOperationException(), default);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_SetsBadRequest()
    {
        var context = CreateContextWithBody();
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Email is required"),
        });

        var handled = await _handler.TryHandleAsync(context, exception, default);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_GroupsErrorsByProperty()
    {
        var context = CreateContextWithBody();
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("Email", "Email is invalid"),
            new ValidationFailure("Name", "Name is required"),
        });

        await _handler.TryHandleAsync(context, exception, default);

        var errors = await ReadErrors(context);
        errors["Email"].Should().BeEquivalentTo("Email is required", "Email is invalid");
        errors["Name"].Should().BeEquivalentTo("Name is required");
    }

    private static DefaultHttpContext CreateContextWithBody()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<Dictionary<string, string[]>> ReadErrors(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
    }
}
