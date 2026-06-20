using System.Net.Http.Headers;
using System.Net.Http.Json;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class RouteApiClient : IRouteApi
{
    private const string UploadClientName = "Upload";

    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;

    public RouteApiClient(HttpClient http, IHttpClientFactory httpClientFactory)
    {
        _http = http;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<RouteListItem>> GetMyRoutesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<RouteListItem>>("api/routes/mine", ct) ?? [];

    public Task<RouteDetail?> GetRouteAsync(Guid id, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<RouteDetail>($"api/routes/{id}", ct);

    public Task<ApiResult> StartRouteAsync(Guid routeId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/start", null, ct);

    public Task<ApiResult> CompleteRouteAsync(Guid routeId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/complete", null, ct);

    public Task<ApiResult> StartStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/start", null, ct);

    public Task<ApiResult> CompleteStopAsync(Guid routeId, Guid stopId, bool isPartial, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/complete", new CompleteStopRequest(isPartial), ct);

    public Task<ApiResult> SkipStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/skip", null, ct);

    public Task<ApiResult> ResumeStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Patch, $"api/routes/{routeId}/stops/{stopId}/resume", null, ct);

    public Task<ApiResult> FailDeliveryAsync(Guid routeId, Guid stopId, FailDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/fail", request, ct);

    public Task<ApiResult> PushLocationAsync(Guid routeId, double latitude, double longitude, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/location", new PushLocationRequest(latitude, longitude), ct);

    public Task<DeliveryPhotoUpload?> CreatePhotoUploadAsync(CancellationToken ct = default) =>
        PostForJsonAsync<DeliveryPhotoUpload>("api/delivery-photos/upload-url", ct);

    public async Task<bool> UploadPhotoAsync(string uploadUrl, byte[] content, string contentType, CancellationToken ct = default)
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var client = _httpClientFactory.CreateClient(UploadClientName);
        using var response = await client.PutAsync(uploadUrl, body, ct);

        return response.IsSuccessStatusCode;
    }

    private async Task<TValue?> PostForJsonAsync<TValue>(string url, CancellationToken ct)
    {
        using var response = await _http.PostAsync(url, null, ct);

        if (!response.IsSuccessStatusCode)
            return default;

        return await response.Content.ReadFromJsonAsync<TValue>(ct);
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return ApiResult.Ok;

        var error = await response.Content.ReadAsStringAsync(ct);
        return ApiResult.Fail(string.IsNullOrWhiteSpace(error) ? "Не удалось выполнить действие." : error);
    }
}
