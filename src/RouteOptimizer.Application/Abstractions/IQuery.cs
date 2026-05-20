using MediatR;

namespace RouteOptimizer.Application.Abstractions;

public interface IQuery<TResult> : IRequest<TResult>
{
}