using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(StubHandler handler, TokenStorage storage)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new AuthService(httpClient, storage);
    }

    [Fact]
    public async Task LoginAsync_Success_StoresTokensAndReturnsTrue()
    {
        var payload = JsonSerializer.Serialize(new AuthTokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
        });
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload),
        });
        var storage = new TokenStorage();
        var service = CreateService(handler, storage);

        var result = await service.LoginAsync("dispatcher@test.com", "secret", default);

        result.Should().BeTrue();
        storage.AccessToken.Should().Be("access-token");
        storage.RefreshToken.Should().Be("refresh-token");
        handler.LastRequestUri!.AbsolutePath.Should().Be("/api/auth/login");
    }

    [Fact]
    public async Task LoginAsync_Unauthorized_ReturnsFalseAndKeepsTokensEmpty()
    {
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var storage = new TokenStorage();
        var service = CreateService(handler, storage);

        var result = await service.LoginAsync("dispatcher@test.com", "wrong", default);

        result.Should().BeFalse();
        storage.AccessToken.Should().BeNull();
        storage.RefreshToken.Should().BeNull();
    }

    [Fact]
    public void Logout_ClearsStoredTokens()
    {
        var storage = new TokenStorage();
        storage.Save("access", "refresh");
        var service = CreateService(new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)), storage);

        service.Logout();

        storage.AccessToken.Should().BeNull();
        storage.RefreshToken.Should().BeNull();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public Uri? LastRequestUri { get; private set; }

        public StubHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }
}
