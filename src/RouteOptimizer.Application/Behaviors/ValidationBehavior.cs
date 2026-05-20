using FluentValidation;
using MediatR;

namespace RouteOptimizer.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }


    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var result = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(request, ct)));

        var errors = result.SelectMany(r => r.Errors)
            .Where(e => !string.IsNullOrEmpty(e.ErrorMessage))
            .ToList();

        if (errors.Any()) throw new ValidationException(errors);

        return await next();
    }
}