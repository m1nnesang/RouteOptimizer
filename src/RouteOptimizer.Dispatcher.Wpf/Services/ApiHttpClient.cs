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

    public ApiHttpClient(HttpClient httpClient, TokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var token = _tokenStorage.AccessToken;
        if (token is null)
            throw new InvalidOperationException("Access token is not set");

        _httpClient.DefaultRequestHeaders.Authorization = new
            AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);

        var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);

        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };


    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url,
        TRequest body, CancellationToken ct = default)
    {
        var token = _tokenStorage.AccessToken;
        if (token is null)
            throw new InvalidOperationException("Access token is not set");

        _httpClient.DefaultRequestHeaders.Authorization = new
            AuthenticationHeaderValue("Bearer", token);

        var jsonBody = JsonSerializer.Serialize(body, JsonOptions);

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);

        var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions, ct);

        return result;
    }

    public async Task PostAsync<TRequest>(string url, TRequest body, CancellationToken ct = default)
    {
        var token = _tokenStorage.AccessToken;
        if (token is null)
            throw new InvalidOperationException("Access token is not set");

        _httpClient.DefaultRequestHeaders.Authorization = new
            AuthenticationHeaderValue("Bearer", token);

        var jsonBody = JsonSerializer.Serialize(body, JsonOptions);

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);

        response.EnsureSuccessStatusCode();
    }

    public async Task PutAsync<TRequest>(string url, TRequest body, CancellationToken ct = default)
    {
        SetAuthorization();

        var jsonBody = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(url, content, ct);

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        SetAuthorization();

        var response = await _httpClient.DeleteAsync(url, ct);

        response.EnsureSuccessStatusCode();
    }

    private void SetAuthorization()
    {
        var token = _tokenStorage.AccessToken;
        if (token is null)
            throw new InvalidOperationException("Access token is not set");

        _httpClient.DefaultRequestHeaders.Authorization = new
            AuthenticationHeaderValue("Bearer", token);
    }
}
