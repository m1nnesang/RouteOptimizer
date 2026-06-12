using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Drivers.DriverShifts.EndShift;

public class EndShiftCommandHandler : IRequestHandler<EndShiftCommand, Result>
{
    private readonly IDriverShiftRepository _driverShiftRepository;

    public EndShiftCommandHandler(IDriverShiftRepository driverShiftRepository) => (_driverShiftRepository) = (driverShiftRepository);

    public async Task<Result> Handle(EndShiftCommand request, CancellationToken ct)
    {

        var shift = await _driverShiftRepository.GetByIdAsync(request.ShiftId, ct);

        if (shift is null)
            throw new NotFoundException("Shift is not found");

        if (shift.DriverId != request.DriverId)
            return Result.Failure("Access denied");

        try
        {
            shift.End();
        }

        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();

    }
}
