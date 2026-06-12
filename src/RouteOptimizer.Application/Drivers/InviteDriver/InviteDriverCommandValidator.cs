using FluentValidation;

namespace RouteOptimizer.Application.Drivers.InviteDriver;

public class InviteDriverCommandValidator : AbstractValidator<InviteDriverCommand>
{
    public InviteDriverCommandValidator()
    {
        RuleFor(m => m.Email).NotEmpty().EmailAddress().WithMessage("Please enter a valid email address");
        RuleFor(m => m.FirstName).NotEmpty().WithMessage("Please enter a valid first name");
        RuleFor(m => m.LastName).NotEmpty().WithMessage("Please enter a valid last name");
        RuleFor(m => m.PhoneNumber).NotEmpty().WithMessage("Please enter a valid phone number");
        RuleFor(m => m.WarehouseId).NotEmpty().WithMessage("Please enter a valid warehouse id");
        RuleFor(m => m.LicenseCategory).NotEmpty().WithMessage("Please provide a license category");
    }
}
