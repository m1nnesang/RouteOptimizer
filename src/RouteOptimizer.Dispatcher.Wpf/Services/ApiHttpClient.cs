using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class ApiHttpClient : IApiHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorage _tokenStorage;
    private readonly IAuthService _authService;
    private readonly ISessionNotifier _sessionNotifier;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiHttpClient(HttpClient httpClient, TokenStorage tokenStorage,
        IAuthService authService, ISessionNotifier sessionNotifier)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
        _authService = authService;
        _sessionNotifier = sessionNotifier;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, url), ct);

        return await DeserializeAsync<T>(response, ct);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url,
        TRequest body, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => CreateJsonRequest(HttpMethod.Post, url, body), ct);

        return await DeserializeAsync<TResponse>(response, ct);
    }

    public async Task PostAsync<TRequest>(string url, TRequest body, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => CreateJsonRequest(HttpMethod.Post, url, body), ct);
    }

    public async Task PostAsync(string url, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, url), ct);
    }

    public async Task PutAsync<TRequest>(string url, TRequest body, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => CreateJsonRequest(HttpMethod.Put, url, body), ct);
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, url), ct);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var token = _tokenStorage.AccessToken;
        if (token is null)
        {
            _sessionNotifier.NotifySessionExpired();
            throw new SessionExpiredException();
        }

        var response = await SendOnceAsync(requestFactory, token, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            if (!await TryRefreshAsync(token, ct))
            {
                _sessionNotifier.NotifySessionExpired();
                throw new SessionExpiredException();
            }

            response = await SendOnceAsync(requestFactory, _tokenStorage.AccessToken, ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException(ExtractErrorMessage(body, status));
        }

        return response;
    }

    private static string ExtractErrorMessage(string body, HttpStatusCode status)
    {
        if (string.IsNullOrWhiteSpace(body))
            return $"Request failed ({(int)status}).";

        body = body.Trim();

        try
        {
            if (body.StartsWith('"'))
                return JsonSerializer.Deserialize<string>(body, JsonOptions) ?? body;

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.GetString() is { } d)
                return d;
            if (doc.RootElement.TryGetProperty("title", out var title) && title.GetString() is { } t)
                return t;
        }
        catch (JsonException)
        {
        }

        return body;
    }

    private Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> requestFactory, string? token, CancellationToken ct)
    {
        var request = requestFactory();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private async Task<bool> TryRefreshAsync(string staleToken, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (_tokenStorage.AccessToken != staleToken)
                return _tokenStorage.AccessToken is not null;

            return await _authService.RefreshAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static HttpRequestMessage CreateJsonRequest<TRequest>(
        HttpMethod method, string url, TRequest body)
    {
        var jsonBody = JsonSerializer.Serialize(body, JsonOptions);
        return new HttpRequestMessage(method, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }
}
