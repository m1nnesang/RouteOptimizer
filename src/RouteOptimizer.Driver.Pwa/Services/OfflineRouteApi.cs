using RouteOptimizer.Driver.Pwa.Common;
using RouteOptimizer.Driver.Pwa.Models;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class OfflineRouteApi : IRouteApi, IOutboxFlusher
{
    private readonly RouteApiClient _inner;
    private readonly IOfflineStore _store;
    private readonly IConnectivity _connectivity;

    public OfflineRouteApi(RouteApiClient inner, IOfflineStore store, IConnectivity connectivity)
    {
        _inner = inner;
        _store = store;
        _connectivity = connectivity;
    }

    public async Task<IReadOnlyList<RouteListItem>> GetMyRoutesAsync(CancellationToken ct = default)
    {
        if (!_connectivity.IsOnline)
            return await _store.GetRoutesAsync();

        try
        {
            var routes = await _inner.GetMyRoutesAsync(ct);
            await _store.SaveRoutesAsync(routes);
            return routes;
        }
        catch (HttpRequestException)
        {
            return await _store.GetRoutesAsync();
        }
    }

    public async Task<RouteDetail?> GetRouteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_connectivity.IsOnline)
            return await _store.GetRouteAsync(id);

        try
        {
            var route = await _inner.GetRouteAsync(id, ct);

            if (route is not null)
                await _store.SaveRouteAsync(route);

            return route;
        }
        catch (HttpRequestException)
        {
            return await _store.GetRouteAsync(id);
        }
    }

    public Task<ApiResult> StartRouteAsync(Guid routeId, string? idempotencyKey = null, CancellationToken ct = default) =>
        ExecuteAsync(NewItem(OutboxKind.StartRoute, routeId),
            key => _inner.StartRouteAsync(routeId, key, ct),
            route => OfflineRouteMutator.StartRoute(route));

    public Task<ApiResult> CompleteRouteAsync(Guid routeId, string? idempotencyKey = null, CancellationToken ct = default) =>
        ExecuteAsync(NewItem(OutboxKind.CompleteRoute, routeId),
            key => _inner.CompleteRouteAsync(routeId, key, ct),
            route => OfflineRouteMutator.CompleteRoute(route));

    public Task<ApiResult> StartStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        _inner.StartStopAsync(routeId, stopId, ct);

    public Task<ApiResult> CompleteStopAsync(Guid routeId, Guid stopId, bool isPartial, CancellationToken ct = default) =>
        _inner.CompleteStopAsync(routeId, stopId, isPartial, ct);

    public Task<ApiResult> SkipStopAsync(Guid routeId, Guid stopId, string? idempotencyKey = null, CancellationToken ct = default) =>
        ExecuteAsync(NewItem(OutboxKind.SkipStop, routeId, stopId),
            key => _inner.SkipStopAsync(routeId, stopId, key, ct),
            route => OfflineRouteMutator.SkipStop(route, stopId));

    public Task<ApiResult> ResumeStopAsync(Guid routeId, Guid stopId, string? idempotencyKey = null, CancellationToken ct = default) =>
        ExecuteAsync(NewItem(OutboxKind.ResumeStop, routeId, stopId),
            key => _inner.ResumeStopAsync(routeId, stopId, key, ct),
            route => OfflineRouteMutator.ResumeStop(route, stopId));

    public Task<ApiResult> FailDeliveryAsync(Guid routeId, Guid stopId, FailDeliveryRequest request, CancellationToken ct = default) =>
        _inner.FailDeliveryAsync(routeId, stopId, request, ct);

    public Task<ApiResult> DeliverOrderAsync(Guid routeId, Guid stopId, Guid orderId, string? idempotencyKey = null, CancellationToken ct = default) =>
        ExecuteAsync(NewItem(OutboxKind.DeliverOrder, routeId, stopId, orderId),
            key => _inner.DeliverOrderAsync(routeId, stopId, orderId, key, ct),
            route => OfflineRouteMutator.DeliverOrder(route, stopId, orderId));

    public Task<ApiResult> FailOrderAsync(Guid routeId, Guid stopId, Guid orderId, FailDeliveryRequest request, string? idempotencyKey = null, CancellationToken ct = default) =>
        ExecuteAsync(
            NewItem(OutboxKind.FailOrder, routeId, stopId, orderId) with
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                FailureReason = request.FailureReason,
                Notes = request.Notes,
                PhotoKey = request.PhotoKey
            },
            key => _inner.FailOrderAsync(routeId, stopId, orderId, request, key, ct),
            route => OfflineRouteMutator.FailOrder(route, stopId, orderId));

    public async Task<ApiResult> PushLocationAsync(Guid routeId, double latitude, double longitude, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var item = NewItem(OutboxKind.Location, routeId) with
        {
            Latitude = latitude,
            Longitude = longitude
        };

        if (_connectivity.IsOnline)
        {
            try
            {
                return await _inner.PushLocationAsync(routeId, latitude, longitude, item.Id.ToString(), ct);
            }
            catch (HttpRequestException)
            {
            }
        }

        await _store.EnqueueAsync(item);

        return ApiResult.Ok;
    }

    public Task<DeliveryPhotoUpload?> CreatePhotoUploadAsync(CancellationToken ct = default) =>
        _inner.CreatePhotoUploadAsync(ct);

    public Task<bool> UploadPhotoAsync(string uploadUrl, byte[] content, string contentType, CancellationToken ct = default) =>
        _inner.UploadPhotoAsync(uploadUrl, content, contentType, ct);

    public Task<int> PendingCountAsync() => _store.CountAsync();

    public async Task<int> FlushAsync(CancellationToken ct = default)
    {
        if (!_connectivity.IsOnline)
            return await _store.CountAsync();

        foreach (var item in await _store.GetOutboxAsync())
        {
            try
            {
                var result = await SendAsync(item, ct);

                if (result.Success || IsPermanentRejection(result))
                    await _store.RemoveAsync(item.Id);
                else
                    break;
            }
            catch (HttpRequestException)
            {
                break;
            }
        }

        return await _store.CountAsync();
    }

    private Task<ApiResult> SendAsync(OutboxItem item, CancellationToken ct)
    {
        var key = item.Id.ToString();

        return item.Kind switch
        {
            OutboxKind.DeliverOrder => _inner.DeliverOrderAsync(item.RouteId, item.StopId!.Value, item.OrderId!.Value, key, ct),
            OutboxKind.FailOrder => _inner.FailOrderAsync(item.RouteId, item.StopId!.Value, item.OrderId!.Value,
                new FailDeliveryRequest(item.Latitude ?? 0, item.Longitude ?? 0, item.FailureReason ?? "Other", item.Notes, item.PhotoKey), key, ct),
            OutboxKind.SkipStop => _inner.SkipStopAsync(item.RouteId, item.StopId!.Value, key, ct),
            OutboxKind.ResumeStop => _inner.ResumeStopAsync(item.RouteId, item.StopId!.Value, key, ct),
            OutboxKind.StartRoute => _inner.StartRouteAsync(item.RouteId, key, ct),
            OutboxKind.CompleteRoute => _inner.CompleteRouteAsync(item.RouteId, key, ct),
            OutboxKind.Location => _inner.PushLocationAsync(item.RouteId, item.Latitude ?? 0, item.Longitude ?? 0, key, ct),
            _ => Task.FromResult(ApiResult.Ok)
        };
    }

    private static bool IsPermanentRejection(ApiResult result) =>
        result.StatusCode is >= 400 and < 500 and not 408 and not 429;

    private async Task<ApiResult> ExecuteAsync(OutboxItem item, Func<string, Task<ApiResult>> online, Func<RouteDetail, RouteDetail> mutate)
    {
        if (_connectivity.IsOnline)
        {
            try
            {
                return await online(item.Id.ToString());
            }
            catch (HttpRequestException)
            {
            }
        }

        await _store.EnqueueAsync(item);

        var cached = await _store.GetRouteAsync(item.RouteId);

        if (cached is not null)
            await _store.SaveRouteAsync(mutate(cached));

        return ApiResult.Ok;
    }

    private static OutboxItem NewItem(OutboxKind kind, Guid routeId, Guid? stopId = null, Guid? orderId = null) =>
        new(Guid.NewGuid(), kind, routeId, stopId, orderId, null, null, null, null, null, DateTime.UtcNow);
}
