using FluentValidation;

namespace RouteOptimizer.Application.Routes.Stops.FailDelivery;

public class FailDeliveryValidator : AbstractValidator<FailDeliveryCommand>
{
    public FailDeliveryValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.StopId).NotEmpty();
        RuleFor(x=> x.DriverId).NotEmpty();

        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
    }
}
