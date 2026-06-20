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

    public Task<ApiResult> StartRouteAsync(Guid routeId, CancellationToken ct = default) =>
        ExecuteAsync(() => _inner.StartRouteAsync(routeId, ct),
            NewItem(OutboxKind.StartRoute, routeId),
            route => OfflineRouteMutator.StartRoute(route));

    public Task<ApiResult> CompleteRouteAsync(Guid routeId, CancellationToken ct = default) =>
        ExecuteAsync(() => _inner.CompleteRouteAsync(routeId, ct),
            NewItem(OutboxKind.CompleteRoute, routeId),
            route => OfflineRouteMutator.CompleteRoute(route));

    public Task<ApiResult> StartStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        _inner.StartStopAsync(routeId, stopId, ct);

    public Task<ApiResult> CompleteStopAsync(Guid routeId, Guid stopId, bool isPartial, CancellationToken ct = default) =>
        _inner.CompleteStopAsync(routeId, stopId, isPartial, ct);

    public Task<ApiResult> SkipStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        ExecuteAsync(() => _inner.SkipStopAsync(routeId, stopId, ct),
            NewItem(OutboxKind.SkipStop, routeId, stopId),
            route => OfflineRouteMutator.SkipStop(route, stopId));

    public Task<ApiResult> ResumeStopAsync(Guid routeId, Guid stopId, CancellationToken ct = default) =>
        ExecuteAsync(() => _inner.ResumeStopAsync(routeId, stopId, ct),
            NewItem(OutboxKind.ResumeStop, routeId, stopId),
            route => OfflineRouteMutator.ResumeStop(route, stopId));

    public Task<ApiResult> FailDeliveryAsync(Guid routeId, Guid stopId, FailDeliveryRequest request, CancellationToken ct = default) =>
        _inner.FailDeliveryAsync(routeId, stopId, request, ct);

    public Task<ApiResult> DeliverOrderAsync(Guid routeId, Guid stopId, Guid orderId, CancellationToken ct = default) =>
        ExecuteAsync(() => _inner.DeliverOrderAsync(routeId, stopId, orderId, ct),
            NewItem(OutboxKind.DeliverOrder, routeId, stopId, orderId),
            route => OfflineRouteMutator.DeliverOrder(route, stopId, orderId));

    public Task<ApiResult> FailOrderAsync(Guid routeId, Guid stopId, Guid orderId, FailDeliveryRequest request, CancellationToken ct = default) =>
        ExecuteAsync(() => _inner.FailOrderAsync(routeId, stopId, orderId, request, ct),
            NewItem(OutboxKind.FailOrder, routeId, stopId, orderId) with
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                FailureReason = request.FailureReason,
                Notes = request.Notes,
                PhotoKey = request.PhotoKey
            },
            route => OfflineRouteMutator.FailOrder(route, stopId, orderId));

    public async Task<ApiResult> PushLocationAsync(Guid routeId, double latitude, double longitude, CancellationToken ct = default)
    {
        if (_connectivity.IsOnline)
        {
            try
            {
                return await _inner.PushLocationAsync(routeId, latitude, longitude, ct);
            }
            catch (HttpRequestException)
            {
            }
        }

        await _store.EnqueueAsync(NewItem(OutboxKind.Location, routeId) with
        {
            Latitude = latitude,
            Longitude = longitude
        });

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

    private Task<ApiResult> SendAsync(OutboxItem item, CancellationToken ct) => item.Kind switch
    {
        OutboxKind.DeliverOrder => _inner.DeliverOrderAsync(item.RouteId, item.StopId!.Value, item.OrderId!.Value, ct),
        OutboxKind.FailOrder => _inner.FailOrderAsync(item.RouteId, item.StopId!.Value, item.OrderId!.Value,
            new FailDeliveryRequest(item.Latitude ?? 0, item.Longitude ?? 0, item.FailureReason ?? "Other", item.Notes, item.PhotoKey), ct),
        OutboxKind.SkipStop => _inner.SkipStopAsync(item.RouteId, item.StopId!.Value, ct),
        OutboxKind.ResumeStop => _inner.ResumeStopAsync(item.RouteId, item.StopId!.Value, ct),
        OutboxKind.StartRoute => _inner.StartRouteAsync(item.RouteId, ct),
        OutboxKind.CompleteRoute => _inner.CompleteRouteAsync(item.RouteId, ct),
        OutboxKind.Location => _inner.PushLocationAsync(item.RouteId, item.Latitude ?? 0, item.Longitude ?? 0, ct),
        _ => Task.FromResult(ApiResult.Ok)
    };

    private static bool IsPermanentRejection(ApiResult result) => !result.Success;

    private async Task<ApiResult> ExecuteAsync(Func<Task<ApiResult>> online, OutboxItem item, Func<RouteDetail, RouteDetail> mutate)
    {
        if (_connectivity.IsOnline)
        {
            try
            {
                return await online();
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
