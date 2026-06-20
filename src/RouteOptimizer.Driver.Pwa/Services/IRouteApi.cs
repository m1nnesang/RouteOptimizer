using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public interface IRouteApi
{
    Task<IReadOnlyList<RouteListItem>> GetMyRoutesAsync(CancellationToken ct = default);

    Task<RouteDetail?> GetRouteAsync(Guid id, CancellationToken ct = default);

    Task<ApiResult> StartRouteAsync(Guid routeId, CancellationToken ct = default);

    Task<ApiResult> CompleteRouteAsync(Guid routeId, CancellationToken ct = default);

    Task<ApiResult> StartStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default);

    Task<ApiResult> CompleteStopAsync(Guid routeId, Guid stopId, bool isPartial, CancellationToken ct = default);

    Task<ApiResult> SkipStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default);

    Task<ApiResult> ResumeStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default);

    Task<ApiResult> FailDeliveryAsync(Guid routeId, Guid stopId, FailDeliveryRequest request, CancellationToken ct = default);

    Task<ApiResult> PushLocationAsync(Guid routeId, double latitude, double longitude, CancellationToken ct = default);

    Task<DeliveryPhotoUpload?> CreatePhotoUploadAsync(CancellationToken ct = default);

    Task<bool> UploadPhotoAsync(string uploadUrl, byte[] content, string contentType, CancellationToken ct = default);
}
