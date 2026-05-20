using FluentValidation;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Orders.CreateBusinessOrder;

public class CreateBusinessOrderCommandValidator : AbstractValidator<CreateBusinessOrderCommand>
{
    public CreateBusinessOrderCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("WarehouseId is required");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("CompanyName is required");
        RuleFor(x => x.ContactPerson).NotEmpty().WithMessage("ContactPerson is required");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required");
        RuleFor(x => x.Postcode).NotEmpty().WithMessage("Postcode is required");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumber is required");
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).WithMessage("Weight must be greater than or equal to 0");
        RuleFor(x => x.Volume).GreaterThanOrEqualTo(0).WithMessage("Volume must be greater than or equal to 0");
        RuleFor(x => x.End)
            .GreaterThan(x => x.Start)
            .WithMessage("Window end must be after window start");
        RuleFor(x => x.WindowStrictness)
            .NotEmpty()
            .Must(x => Enum.TryParse<WindowStrictness>(x, out _))
            .WithMessage("Invalid WindowStrictness");
        RuleFor(x => x.CargoType).NotEmpty().Must(x => Enum.IsDefined(typeof(CargoType), x)).WithMessage("Invalid CargoType");
    }
}
