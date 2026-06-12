using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Infrastructure.Persistence;

namespace RouteOptimizer.API.Middleware;

public class IdempotencyMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var clientKey))
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var compositeKey = $"{userId}:{context.Request.Path}:{clientKey}";

        await using var scope = scopeFactory.CreateAsyncScope();
        var idempotencyDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await idempotencyDb.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == compositeKey);

        if (existing is not null)
        {
            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.Response);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            using var reader = new StreamReader(buffer, leaveOpen: true);
            await next(context);

            buffer.Position = 0;
            var responseBody = await reader.ReadToEndAsync();

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var record = IdempotencyRecord.Create(compositeKey, context.Response.StatusCode, responseBody);
                idempotencyDb.IdempotencyRecords.Add(record);
                try
                {
                    await idempotencyDb.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                }
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
