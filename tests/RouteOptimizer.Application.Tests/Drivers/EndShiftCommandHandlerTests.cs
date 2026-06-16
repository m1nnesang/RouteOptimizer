using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Drivers.DriverShifts.EndShift;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Tests.Drivers;

public class EndShiftCommandHandlerTests
{
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly EndShiftCommandHandler _handler;

    public EndShiftCommandHandlerTests()
    {
        _handler = new EndShiftCommandHandler(_shiftRepository.Object);
    }

    [Fact]
    public async Task Handle_ShiftNotFound_ThrowsNotFoundException()
    {
        _shiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);

        var act = () => _handler.Handle(new EndShiftCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShiftBelongsToDifferentDriver_ReturnsFailure()
    {
        var driverId = Guid.NewGuid();
        var shift = CreateShift(Guid.NewGuid());

        _shiftRepository
            .Setup(x => x.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new EndShiftCommand(shift.Id, driverId), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShiftNotStarted_ReturnsFailure()
    {
        var driverId = Guid.NewGuid();
        var shift = CreateShift(driverId);

        _shiftRepository
            .Setup(x => x.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new EndShiftCommand(shift.Id, driverId), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ActiveShift_EndsShiftAndReturnsSuccess()
    {
        var driverId = Guid.NewGuid();
        var shift = CreateShift(driverId);
        shift.Start();

        _shiftRepository
            .Setup(x => x.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new EndShiftCommand(shift.Id, driverId), default);

        result.IsSuccess.Should().BeTrue();
        shift.EndedAt.Should().NotBeNull();
    }

    private static DriverShift CreateShift(Guid driverId) =>
        DriverShift.Create(driverId, Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow)).Value!;
}
