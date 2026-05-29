using Microsoft.AspNetCore.Diagnostics;
using RouteOptimizer.Application.Exceptions;

namespace RouteOptimizer.API.Middleware;

public sealed class NotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not NotFoundException notFoundException)
            return false;

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await context.Response.WriteAsJsonAsync(new { error = notFoundException.Message }, ct);

        return true;
    }
}
