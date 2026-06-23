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

    public async Task<IReadOnlyList<RouteListItem>> GetMyRoutesAsync(DateOnly? date = null, CancellationToken ct = default)
    {
        var url = date is { } d ? $"api/routes/mine?date={d:yyyy-MM-dd}" : "api/routes/mine";
        return await _http.GetFromJsonAsync<IReadOnlyList<RouteListItem>>(url, ct) ?? [];
    }

    public Task<RouteDetail?> GetRouteAsync(Guid id, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<RouteDetail>($"api/routes/{id}", ct);

    public Task<ApiResult> StartRouteAsync(Guid routeId, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/start", null, idempotencyKey, ct);

    public Task<ApiResult> CompleteRouteAsync(Guid routeId, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/complete", null, idempotencyKey, ct);

    public Task<ApiResult> StartStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/start", null, null, ct);

    public Task<ApiResult> CompleteStopAsync(Guid routeId, Guid stopId, bool isPartial, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/complete", new CompleteStopRequest(isPartial), null, ct);

    public Task<ApiResult> SkipStopAsync(Guid routeId, Guid stopId, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/skip", null, idempotencyKey, ct);

    public Task<ApiResult> ResumeStopAsync(Guid routeId, Guid stopId, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Patch, $"api/routes/{routeId}/stops/{stopId}/resume", null, idempotencyKey, ct);

    public Task<ApiResult> FailDeliveryAsync(Guid routeId, Guid stopId, FailDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/fail", request, null, ct);

    public Task<ApiResult> DeliverOrderAsync(Guid routeId, Guid stopId, Guid orderId, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/orders/{orderId}/deliver", null, idempotencyKey, ct);

    public Task<ApiResult> FailOrderAsync(Guid routeId, Guid stopId, Guid orderId, FailDeliveryRequest request, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/stops/{stopId}/orders/{orderId}/fail", request, idempotencyKey, ct);

    public Task<ApiResult> PushLocationAsync(Guid routeId, double latitude, double longitude, string? idempotencyKey = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"api/routes/{routeId}/location", new PushLocationRequest(latitude, longitude), idempotencyKey, ct);

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

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body, string? idempotencyKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);

        if (idempotencyKey is not null)
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return new ApiResult(true, null, (int)response.StatusCode);

        var error = await response.Content.ReadAsStringAsync(ct);
        return ApiResult.Fail(string.IsNullOrWhiteSpace(error) ? "The action could not be completed." : error, (int)response.StatusCode);
    }
}
