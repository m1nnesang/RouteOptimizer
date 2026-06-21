using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class AuthService : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ITokenStore _tokenStore;

    public AuthService(HttpClient http, ITokenStore tokenStore)
    {
        _http = http;
        _tokenStore = tokenStore;
    }

    public async Task<LoginOutcome> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), ct);
        }
        catch (HttpRequestException)
        {
            return LoginOutcome.NetworkError;
        }

        if (!response.IsSuccessStatusCode)
            return MapFailure(response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions, ct);
        if (token is null)
            return LoginOutcome.ServerError;

        await _tokenStore.SaveAsync(token.AccessToken, token.RefreshToken);
        return LoginOutcome.Success;
    }

    private static LoginOutcome MapFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => LoginOutcome.InvalidCredentials,
        HttpStatusCode.TooManyRequests => LoginOutcome.RateLimited,
        >= HttpStatusCode.InternalServerError => LoginOutcome.ServerError,
        _ => LoginOutcome.InvalidCredentials
    };

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        var refreshToken = await _tokenStore.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest(refreshToken), ct);
        }
        catch (HttpRequestException)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            await _tokenStore.ClearAsync();
            return false;
        }

        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions, ct);
        if (token is null)
            return false;

        await _tokenStore.SaveAsync(token.AccessToken, token.RefreshToken);
        return true;
    }

    public Task LogoutAsync() => _tokenStore.ClearAsync();
}
