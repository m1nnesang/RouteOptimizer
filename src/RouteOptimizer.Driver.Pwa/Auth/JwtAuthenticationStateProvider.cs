using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using RouteOptimizer.Driver.Pwa.Services;

namespace RouteOptimizer.Driver.Pwa.Auth;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string NameClaim = "email";

    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ITokenStore _tokenStore;
    private readonly IAuthService _authService;

    public JwtAuthenticationStateProvider(ITokenStore tokenStore, IAuthService authService)
    {
        _tokenStore = tokenStore;
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
            return Anonymous;

        var claims = JwtParser.ParseClaims(token);

        if (JwtParser.IsExpired(claims))
        {
            if (!await _authService.RefreshAsync())
                return Anonymous;

            token = await _tokenStore.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return Anonymous;

            claims = JwtParser.ParseClaims(token);
        }

        var identity = new ClaimsIdentity(claims, "jwt", NameClaim, ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthentication() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void NotifyUserLogout() =>
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
}
