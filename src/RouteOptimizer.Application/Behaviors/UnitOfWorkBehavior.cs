using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork, IPublisher publisher)
    {
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var response = await next();

        if (response is not Result { IsFailure: true })
        {
            var events = _unitOfWork.GetPendingDomainEvents();
            _unitOfWork.ClearAllDomainEvents();
            await _unitOfWork.SaveChangesAsync(ct);
            foreach (var evt in events)
                await _publisher.Publish(evt, ct);
        }

        return response;
    }
}
