using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Drivers.DriverShifts.EndShift;

public class EndShiftCommandHandler : IRequestHandler<EndShiftCommand, Result>
{
    private readonly IDriverShiftRepository _driverShiftRepository;
    private readonly ICurrentUser _currentUser;

    public EndShiftCommandHandler(IDriverShiftRepository driverShiftRepository, ICurrentUser currentUser) =>
        (_driverShiftRepository, _currentUser) = (driverShiftRepository, currentUser);

    public async Task<Result> Handle(EndShiftCommand request, CancellationToken ct)
    {

        var shift = await _driverShiftRepository.GetByIdAsync(request.ShiftId, ct);

        if (shift is null)
            throw new NotFoundException("Shift is not found");

        if (_currentUser.WarehouseId is { } warehouseId && shift.WarehouseId != warehouseId)
            throw new NotFoundException("Shift is not found");

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
