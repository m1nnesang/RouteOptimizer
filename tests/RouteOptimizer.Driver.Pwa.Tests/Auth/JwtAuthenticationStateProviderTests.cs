using System.Security.Claims;
using FluentAssertions;
using Moq;
using RouteOptimizer.Driver.Pwa.Auth;
using RouteOptimizer.Driver.Pwa.Services;
using RouteOptimizer.Driver.Pwa.Tests.TestSupport;

namespace RouteOptimizer.Driver.Pwa.Tests.Auth;

public class JwtAuthenticationStateProviderTests
{
    private readonly Mock<ITokenStore> _tokenStore = new();
    private readonly Mock<IAuthService> _authService = new();

    private JwtAuthenticationStateProvider CreateSut() => new(_tokenStore.Object, _authService.Object);

    [Fact]
    public async Task GetAuthenticationStateAsync_NoToken_ReturnsAnonymous()
    {
        _tokenStore.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync((string?)null);

        var state = await CreateSut().GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ValidToken_ReturnsAuthenticatedUserWithRole()
    {
        _tokenStore.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync(JwtFactory.Valid());

        var state = await CreateSut().GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.Identity.Name.Should().Be("driver@example.com");
        state.User.IsInRole("Driver").Should().BeTrue();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ExpiredToken_RefreshSucceeds_UsesNewToken()
    {
        _tokenStore.SetupSequence(s => s.GetAccessTokenAsync())
            .ReturnsAsync(JwtFactory.Expired())
            .ReturnsAsync(JwtFactory.Valid());
        _authService.Setup(s => s.RefreshAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var state = await CreateSut().GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Driver");
        _authService.Verify(s => s.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ExpiredToken_RefreshFails_ReturnsAnonymous()
    {
        _tokenStore.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync(JwtFactory.Expired());
        _authService.Setup(s => s.RefreshAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var state = await CreateSut().GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }
}
