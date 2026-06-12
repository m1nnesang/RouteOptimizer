using Serilog.Context;

namespace RouteOptimizer.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context.Request);

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private const int MaxLength = 64;

    private static string GetOrCreateCorrelationId(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var value))
            return Guid.NewGuid().ToString();

        var incoming = value.ToString();

        if (string.IsNullOrWhiteSpace(incoming) || incoming.Length > MaxLength)
            return Guid.NewGuid().ToString();

        return incoming;
    }
}
