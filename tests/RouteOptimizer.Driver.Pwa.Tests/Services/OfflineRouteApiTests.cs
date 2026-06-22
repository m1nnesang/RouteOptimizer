using System.Net;
using FluentAssertions;
using Moq;
using RouteOptimizer.Driver.Pwa.Models;
using RouteOptimizer.Driver.Pwa.Services;
using RouteOptimizer.Driver.Pwa.Tests.TestSupport;

namespace RouteOptimizer.Driver.Pwa.Tests.Services;

public class OfflineRouteApiTests
{
    private readonly InMemoryOfflineStore _store = new();

    private static OfflineRouteApi CreateSut(
        StubHttpMessageHandler handler, IOfflineStore store, IConnectivity connectivity)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(handler.CreateClient());
        var inner = new RouteApiClient(handler.CreateClient(), factory.Object);
        return new OfflineRouteApi(inner, store, connectivity);
    }

    [Fact]
    public async Task GetMyRoutesAsync_Offline_ReturnsCachedList()
    {
        await _store.SaveRoutesAsync([ListItem()]);
        var sut = CreateSut(StubHttpMessageHandler.Throws(), _store, new FakeConnectivity(false));

        var routes = await sut.GetMyRoutesAsync();

        routes.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeliverOrderAsync_Offline_EnqueuesAndMutatesCache()
    {
        var (route, stop, order) = SingleOrderRoute();
        await _store.SaveRouteAsync(route);
        var sut = CreateSut(StubHttpMessageHandler.Throws(), _store, new FakeConnectivity(false));

        var result = await sut.DeliverOrderAsync(route.Id, stop.Id, order.OrderId);

        result.Success.Should().BeTrue();
        _store.Outbox.Should().ContainSingle(i => i.Kind == OutboxKind.DeliverOrder);
        var cached = await _store.GetRouteAsync(route.Id);
        cached!.Stops.Single().Status.Should().Be("Completed");
    }

    [Fact]
    public async Task SkipStopAsync_Offline_EnqueuesAndMarksStopSkipped()
    {
        var (route, stop, _) = SingleOrderRoute();
        await _store.SaveRouteAsync(route);
        var sut = CreateSut(StubHttpMessageHandler.Throws(), _store, new FakeConnectivity(false));

        await sut.SkipStopAsync(route.Id, stop.Id);

        _store.Outbox.Should().ContainSingle(i => i.Kind == OutboxKind.SkipStop);
        var cached = await _store.GetRouteAsync(route.Id);
        cached!.Stops.Single().Status.Should().Be("Skipped");
    }

    [Fact]
    public async Task PushLocationAsync_Offline_EnqueuesLocationWithCoordinates()
    {
        var sut = CreateSut(StubHttpMessageHandler.Throws(), _store, new FakeConnectivity(false));

        var result = await sut.PushLocationAsync(Guid.NewGuid(), 52.1, 21.2);

        result.Success.Should().BeTrue();
        var item = _store.Outbox.Should().ContainSingle().Subject;
        item.Kind.Should().Be(OutboxKind.Location);
        item.Latitude.Should().Be(52.1);
        item.Longitude.Should().Be(21.2);
    }

    [Fact]
    public async Task DeliverOrderAsync_Online_NetworkError_FallsBackToOfflineQueue()
    {
        var (route, stop, order) = SingleOrderRoute();
        await _store.SaveRouteAsync(route);
        var sut = CreateSut(StubHttpMessageHandler.Throws(), _store, new FakeConnectivity(true));

        var result = await sut.DeliverOrderAsync(route.Id, stop.Id, order.OrderId);

        result.Success.Should().BeTrue();
        _store.Outbox.Should().ContainSingle(i => i.Kind == OutboxKind.DeliverOrder);
    }

    [Fact]
    public async Task DeliverOrderAsync_Online_Success_DoesNotEnqueue()
    {
        var (route, stop, order) = SingleOrderRoute();
        await _store.SaveRouteAsync(route);
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.OK), _store, new FakeConnectivity(true));

        var result = await sut.DeliverOrderAsync(route.Id, stop.Id, order.OrderId);

        result.Success.Should().BeTrue();
        _store.Outbox.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushAsync_Offline_ReturnsPendingCountWithoutSending()
    {
        await _store.EnqueueAsync(LocationItem());
        var handler = StubHttpMessageHandler.Throws();
        var sut = CreateSut(handler, _store, new FakeConnectivity(false));

        var pending = await sut.FlushAsync();

        pending.Should().Be(1);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushAsync_Online_DrainsOutboxOnSuccess()
    {
        await _store.EnqueueAsync(LocationItem());
        await _store.EnqueueAsync(LocationItem());
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.OK), _store, new FakeConnectivity(true));

        var pending = await sut.FlushAsync();

        pending.Should().Be(0);
        _store.Outbox.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushAsync_Online_TransientError_StopsAndKeepsItem()
    {
        await _store.EnqueueAsync(LocationItem());
        var sut = CreateSut(StubHttpMessageHandler.Throws(), _store, new FakeConnectivity(true));

        var pending = await sut.FlushAsync();

        pending.Should().Be(1);
        _store.Outbox.Should().HaveCount(1);
    }

    [Fact]
    public async Task FlushAsync_Online_PermanentRejection_RemovesItem()
    {
        await _store.EnqueueAsync(LocationItem());
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.BadRequest), _store, new FakeConnectivity(true));

        var pending = await sut.FlushAsync();

        pending.Should().Be(0);
        _store.Outbox.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushAsync_Online_ServerError_StopsAndKeepsItem()
    {
        await _store.EnqueueAsync(LocationItem());
        var sut = CreateSut(StubHttpMessageHandler.Status(HttpStatusCode.InternalServerError), _store, new FakeConnectivity(true));

        var pending = await sut.FlushAsync();

        pending.Should().Be(1);
        _store.Outbox.Should().HaveCount(1);
    }

    [Fact]
    public async Task FlushAsync_SendsIdempotencyKeyMatchingOutboxItemId()
    {
        await _store.EnqueueAsync(LocationItem());
        var item = _store.Outbox.Single();
        var handler = StubHttpMessageHandler.Status(HttpStatusCode.OK);
        var sut = CreateSut(handler, _store, new FakeConnectivity(true));

        await sut.FlushAsync();

        var request = handler.Requests.Single();
        request.Headers.TryGetValues("X-Idempotency-Key", out var values).Should().BeTrue();
        values!.Single().Should().Be(item.Id.ToString());
    }

    private static OutboxItem LocationItem() =>
        new(Guid.NewGuid(), OutboxKind.Location, Guid.NewGuid(), null, null, 1.0, 2.0, null, null, null, DateTime.UtcNow);

    private static RouteListItem ListItem() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "InProgress", 1, null, null, "Driver", "Warehouse");

    private static (RouteDetail Route, RouteStop Stop, StopOrder Order) SingleOrderRoute()
    {
        var order = new StopOrder(Guid.NewGuid(), "Recipient", null, "Individual", "+10000000000", "InTransit");
        var stop = new RouteStop(Guid.NewGuid(), 0, "City", "Street", 52.0, 21.0, "InProgress",
            [order.OrderId], [order]);
        var route = new RouteDetail(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "InProgress", [stop]);
        return (route, stop, order);
    }
}
