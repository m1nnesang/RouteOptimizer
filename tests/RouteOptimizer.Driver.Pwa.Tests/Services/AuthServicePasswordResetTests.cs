using System.Net;
using FluentAssertions;
using Moq;
using RouteOptimizer.Driver.Pwa.Services;
using RouteOptimizer.Driver.Pwa.Tests.TestSupport;

namespace RouteOptimizer.Driver.Pwa.Tests.Services;

public class AuthServicePasswordResetTests
{
    private readonly Mock<ITokenStore> _tokenStore = new();

    private AuthService CreateSut(StubHttpMessageHandler handler) =>
        new(handler.CreateClient(), _tokenStore.Object);

    [Fact]
    public async Task RequestPasswordResetAsync_Success_ReturnsTrue()
    {
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.NoContent));

        var result = await sut.RequestPasswordResetAsync("driver@example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RequestPasswordResetAsync_NetworkError_ReturnsFalse()
    {
        var sut = CreateSut(StubHttpMessageHandler.Throws());

        var result = await sut.RequestPasswordResetAsync("driver@example.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_Success_ReturnsTrue()
    {
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.NoContent));

        var result = await sut.ResetPasswordAsync("token", "NewPassword1!");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_BadRequest_ReturnsFalse()
    {
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.BadRequest));

        var result = await sut.ResetPasswordAsync("bad", "NewPassword1!");

        result.Should().BeFalse();
    }
}
