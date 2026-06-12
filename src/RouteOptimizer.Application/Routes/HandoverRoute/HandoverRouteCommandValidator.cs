using FluentValidation;

namespace RouteOptimizer.Application.Routes.HandoverRoute;

public class HandoverRouteCommandValidator : AbstractValidator<HandoverRouteCommand>
{
    public HandoverRouteCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();

        When(x => x.Type == HandoverType.TransferAll, () =>
        {
            RuleFor(x => x.TargetShiftId)
                .NotEmpty()
                .WithMessage("TargetShiftId is required for TransferAll");
        });

        When(x => x.Type == HandoverType.Distribute, () =>
        {
            RuleFor(x => x.Assignments)
                .NotEmpty()
                .WithMessage("Assignments are required for Distribute");

            RuleForEach(x => x.Assignments).ChildRules(a =>
            {
                a.RuleFor(x => x.ShiftId).NotEmpty();
                a.RuleFor(x => x.StopIds).NotEmpty();
            });
        });
    }
}
