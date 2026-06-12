using FluentValidation;

namespace RouteOptimizer.Application.Vehicles.UpdateVehicle;

public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Type is required");
        RuleFor(x => x.MaxWeightKg).GreaterThan(0).WithMessage("MaxWeightKg must be greater than 0");
        RuleFor(x => x.MaxVolumeM3).GreaterThan(0).WithMessage("MaxVolumeM3 must be greater than 0");
        RuleFor(x => x.LicenseCategory).IsInEnum().WithMessage("Invalid LicenseCategory");
        RuleFor(x => x.AllowedCargoTypes).NotEmpty().WithMessage("At least one CargoType is required");
        RuleForEach(x => x.AllowedCargoTypes).IsInEnum().WithMessage("Invalid CargoType");
    }
}
