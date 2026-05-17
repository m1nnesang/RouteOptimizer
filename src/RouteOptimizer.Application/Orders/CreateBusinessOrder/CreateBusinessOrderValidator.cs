using FluentValidation;

namespace RouteOptimizer.Application.Orders.CreateBusinessOrder;

public class CreateBusinessOrderValidator : AbstractValidator<CreateBusinessOrderCommand>
{
    public CreateBusinessOrderValidator()
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
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End).NotEmpty()
            .GreaterThan(x => x.Start)
            .WithMessage("Window end must be after window start");
    }
}