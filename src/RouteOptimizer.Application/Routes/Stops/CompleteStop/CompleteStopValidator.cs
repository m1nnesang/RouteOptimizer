using FluentValidation;

namespace RouteOptimizer.Application.Routes.Stops.CompleteStop;

public class CompleteStopValidator : AbstractValidator<CompleteStopCommand>
{
    public CompleteStopValidator()
    {
        RuleFor(x => x.StopId).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
    }
}
