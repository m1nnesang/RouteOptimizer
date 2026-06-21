using System.Net;

namespace RouteOptimizer.Driver.Pwa.Tests.TestSupport;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    public List<HttpRequestMessage> Requests { get; } = [];

    public static StubHttpMessageHandler Json(HttpStatusCode status, string json) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

    public static StubHttpMessageHandler Status(HttpStatusCode status) =>
        new(_ => new HttpResponseMessage(status));

    public static StubHttpMessageHandler Throws() =>
        new(_ => throw new HttpRequestException("network down"));

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_responder(request));
    }

    public HttpClient CreateClient() =>
        new(this) { BaseAddress = new Uri("http://localhost") };
}
