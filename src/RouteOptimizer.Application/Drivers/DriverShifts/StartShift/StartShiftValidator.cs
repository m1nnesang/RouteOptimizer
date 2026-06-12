using FluentValidation;

namespace RouteOptimizer.Application.Drivers.DriverShifts.StartShift;

public class StartShiftValidator : AbstractValidator<StartShiftCommand>
{
    public StartShiftValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ShiftDate).NotEmpty().GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));
    }
}
