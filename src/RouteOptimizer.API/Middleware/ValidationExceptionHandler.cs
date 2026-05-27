using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;

namespace RouteOptimizer.API.Middleware;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not ValidationException validationException)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        await context.Response.WriteAsJsonAsync(new { errors });

        return true;
    }
}
