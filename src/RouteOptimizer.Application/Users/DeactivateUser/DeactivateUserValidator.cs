using FluentValidation;

namespace RouteOptimizer.Application.Users.DeactivateUser;

public sealed class DeactivateUserValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
