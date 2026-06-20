using System.Net;
using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Services;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.Services;

public class ApiHttpClientTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<ISessionNotifier> _sessionNotifier = new();

    private ApiHttpClient CreateClient(StubHandler handler, TokenStorage storage)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ApiHttpClient(httpClient, storage, _auth.Object, _sessionNotifier.Object);
    }

    [Fact]
    public async Task GetAsync_NoToken_NotifiesSessionExpiredAndThrows()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.OK), new TokenStorage());

        var act = async () => await client.GetAsync<object>("api/orders");

        await act.Should().ThrowAsync<SessionExpiredException>();
        _sessionNotifier.Verify(s => s.NotifySessionExpired(), Times.Once);
    }

    [Fact]
    public async Task GetAsync_UnauthorizedAndRefreshFails_NotifiesSessionExpiredAndThrows()
    {
        var storage = new TokenStorage();
        storage.Save("stale-token", "refresh-token");
        _auth.Setup(a => a.RefreshAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var client = CreateClient(new StubHandler(HttpStatusCode.Unauthorized), storage);

        var act = async () => await client.GetAsync<object>("api/orders");

        await act.Should().ThrowAsync<SessionExpiredException>();
        _sessionNotifier.Verify(s => s.NotifySessionExpired(), Times.Once);
    }

    [Fact]
    public async Task GetAsync_NetworkFailure_ThrowsHttpRequestExceptionWithoutSessionNotice()
    {
        var storage = new TokenStorage();
        storage.Save("token", "refresh");
        var client = CreateClient(new StubHandler(new HttpRequestException("network down")), storage);

        var act = async () => await client.GetAsync<object>("api/orders");

        await act.Should().ThrowAsync<HttpRequestException>();
        _sessionNotifier.Verify(s => s.NotifySessionExpired(), Times.Never);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _status;
        private readonly Exception? _exception;

        public StubHandler(HttpStatusCode status) => _status = status;
        public StubHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null)
                throw _exception;

            return Task.FromResult(new HttpResponseMessage(_status!.Value)
            {
                Content = new StringContent("{}"),
            });
        }
    }
}
