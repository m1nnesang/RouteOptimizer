using FluentValidation;

namespace RouteOptimizer.Application.Warehouses.UpdateWarehouse;

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required");
        RuleFor(x => x.PostalCode).NotEmpty().WithMessage("PostalCode is required");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required");
    }
}
