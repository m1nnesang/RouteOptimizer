using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Infrastructure.Persistence;

namespace RouteOptimizer.API.Middleware;

public class IdempotencyMiddleware(RequestDelegate next)
  {
      public async Task InvokeAsync(HttpContext context, AppDbContext db)
      {
          if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var key))
          {
              await next(context);
              return;
          }

          var existing = await db.IdempotencyRecords
              .FirstOrDefaultAsync(r => r.Key == key.ToString());

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

          await next(context);

          buffer.Position = 0;
          var responseBody = await new StreamReader(buffer).ReadToEndAsync();

          if (context.Response.StatusCode is >= 200 and < 300)
          {
              var record = IdempotencyRecord.Create(key.ToString(), context.Response.StatusCode, responseBody);
              db.IdempotencyRecords.Add(record);
              await db.SaveChangesAsync();
          }

          buffer.Position = 0;
          await buffer.CopyToAsync(originalBody);
          context.Response.Body = originalBody;
      }
  }
