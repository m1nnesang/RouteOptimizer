using FluentValidation;

namespace RouteOptimizer.Application.Routes;

public class OptimizeRouteValidator : AbstractValidator<OptimizeRouteCommand>
{
    public OptimizeRouteValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
    }
}
