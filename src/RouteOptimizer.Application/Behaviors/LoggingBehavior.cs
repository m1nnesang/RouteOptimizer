using Microsoft.Extensions.Logging;
using System.Diagnostics;
using MediatR;

namespace RouteOptimizer.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    
    public  LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", 
            typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}