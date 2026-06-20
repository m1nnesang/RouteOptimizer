using FluentValidation;

namespace RouteOptimizer.Application.Routes.Orders.FailOrder;

public class FailOrderValidator : AbstractValidator<FailOrderCommand>
{
    public FailOrderValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.StopId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();

        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
    }
}
