using System.Net;
using System.Net.Http.Headers;
using RouteOptimizer.Driver.Pwa.Auth;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;
    private readonly IAuthService _authService;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public JwtAuthorizationMessageHandler(
        ITokenStore tokenStore,
        IAuthService authService,
        JwtAuthenticationStateProvider authStateProvider)
    {
        _tokenStore = tokenStore;
        _authService = authService;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        ApplyToken(request, token);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();

        if (!await TryRefreshAsync(token, cancellationToken))
        {
            _authStateProvider.NotifyUserLogout();
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        var retry = await CloneAsync(request);
        ApplyToken(retry, await _tokenStore.GetAccessTokenAsync());
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task<bool> TryRefreshAsync(string? staleToken, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var current = await _tokenStore.GetAccessTokenAsync();
            if (current != staleToken)
                return !string.IsNullOrEmpty(current);

            return await _authService.RefreshAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static void ApplyToken(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content is not null)
        {
            var buffer = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(buffer);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
