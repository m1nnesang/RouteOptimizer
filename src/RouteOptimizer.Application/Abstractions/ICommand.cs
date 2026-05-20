using MediatR;

namespace RouteOptimizer.Application.Abstractions;

public interface ICommand : IRequest
{
}

public interface ICommand<TResult> : IRequest<TResult>
{
}