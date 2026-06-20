using FluentValidation;

namespace RouteOptimizer.Application.Routes.PushDriverLocation;

public class PushDriverLocationCommandValidator : AbstractValidator<PushDriverLocationCommand>
{
    public PushDriverLocationCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
