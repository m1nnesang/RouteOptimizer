using FluentValidation;

namespace RouteOptimizer.Application.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
  public LoginCommandValidator()
   {
       RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
           .EmailAddress().WithMessage("Email is not valid");

       RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
       RuleFor(x => x.Password).MinimumLength(8).WithMessage("Password must be between 6 and 20 characters");
   }
}
