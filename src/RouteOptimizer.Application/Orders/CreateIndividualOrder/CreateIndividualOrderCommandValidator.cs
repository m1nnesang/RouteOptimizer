using FluentValidation;

namespace RouteOptimizer.Application.Orders.CreateIndividualOrder;

public class CreateIndividualOrderCommandValidator : AbstractValidator<CreateIndividualOrderCommand>
{
    public CreateIndividualOrderCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("WarehouseId is required");
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("CustomerName is required");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required");
        RuleFor(x => x.Postcode).NotEmpty().WithMessage("Postcode is required");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required");
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).WithMessage("Weight must be greater than or equal to 0");
        RuleFor(x => x.Volume).GreaterThanOrEqualTo(0).WithMessage("Volume must be greater than or equal to 0");
        RuleFor(x => x)
            .Must(x => x.Start.HasValue || x.End.HasValue)
            .When(x => !string.IsNullOrEmpty(x.WindowStrictness))
            .WithMessage("At least one of Start or End must be provided when Strictness is set");
    }
}