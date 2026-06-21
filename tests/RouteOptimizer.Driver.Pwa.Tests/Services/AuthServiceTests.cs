using System.Net;
using FluentAssertions;
using Moq;
using RouteOptimizer.Driver.Pwa.Services;
using RouteOptimizer.Driver.Pwa.Tests.TestSupport;

namespace RouteOptimizer.Driver.Pwa.Tests.Services;

public class AuthServiceTests
{
    private const string TokenJson =
        """{"accessToken":"access-123","refreshToken":"refresh-456","expiresAt":"2030-01-01T00:00:00Z"}""";

    private readonly Mock<ITokenStore> _tokenStore = new();

    private AuthService CreateSut(StubHttpMessageHandler handler) =>
        new(handler.CreateClient(), _tokenStore.Object);

    [Fact]
    public async Task LoginAsync_Success_SavesTokensAndReturnsSuccess()
    {
        var sut = CreateSut(StubHttpMessageHandler.Json(HttpStatusCode.OK, TokenJson));

        var result = await sut.LoginAsync("driver@example.com", "pass");

        result.Should().Be(LoginOutcome.Success);
        _tokenStore.Verify(s => s.SaveAsync("access-123", "refresh-456"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Unauthorized_ReturnsInvalidCredentialsAndDoesNotSave()
    {
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.Unauthorized));

        var result = await sut.LoginAsync("driver@example.com", "wrong");

        result.Should().Be(LoginOutcome.InvalidCredentials);
        _tokenStore.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_TooManyRequests_ReturnsRateLimited()
    {
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.TooManyRequests));

        var result = await sut.LoginAsync("driver@example.com", "pass");

        result.Should().Be(LoginOutcome.RateLimited);
    }

    [Fact]
    public async Task LoginAsync_ServerError_ReturnsServerError()
    {
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        var result = await sut.LoginAsync("driver@example.com", "pass");

        result.Should().Be(LoginOutcome.ServerError);
    }

    [Fact]
    public async Task LoginAsync_NetworkError_ReturnsNetworkError()
    {
        var sut = CreateSut(StubHttpMessageHandler.Throws());

        var result = await sut.LoginAsync("driver@example.com", "pass");

        result.Should().Be(LoginOutcome.NetworkError);
    }

    [Fact]
    public async Task RefreshAsync_NoStoredRefreshToken_ReturnsFalseWithoutHttpCall()
    {
        _tokenStore.Setup(s => s.GetRefreshTokenAsync()).ReturnsAsync((string?)null);
        var handler = StubHttpMessageHandler.Throws();

        var result = await CreateSut(handler).RefreshAsync();

        result.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_Success_SavesNewTokens()
    {
        _tokenStore.Setup(s => s.GetRefreshTokenAsync()).ReturnsAsync("old-refresh");
        var sut = CreateSut(StubHttpMessageHandler.Json(HttpStatusCode.OK, TokenJson));

        var result = await sut.RefreshAsync();

        result.Should().BeTrue();
        _tokenStore.Verify(s => s.SaveAsync("access-123", "refresh-456"), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_Rejected_ClearsTokensAndReturnsFalse()
    {
        _tokenStore.Setup(s => s.GetRefreshTokenAsync()).ReturnsAsync("old-refresh");
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.Unauthorized));

        var result = await sut.RefreshAsync();

        result.Should().BeFalse();
        _tokenStore.Verify(s => s.ClearAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ClearsTokenStore()
    {
        await CreateSut(StubHttpMessageHandler.Throws()).LogoutAsync();

        _tokenStore.Verify(s => s.ClearAsync(), Times.Once);
    }
}
