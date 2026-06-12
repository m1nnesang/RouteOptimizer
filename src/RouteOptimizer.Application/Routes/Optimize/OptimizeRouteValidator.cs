using FluentValidation;

namespace RouteOptimizer.Application.Routes.Optimize;

public class OptimizeRouteValidator : AbstractValidator<OptimizeRouteCommand>
{
    public OptimizeRouteValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
    }
}
