using FluentValidation;

namespace RouteOptimizer.Application.Routes.GetRoutes;

public class GetRoutesQueryValidator : AbstractValidator<GetRoutesQuery>
{
    public GetRoutesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
