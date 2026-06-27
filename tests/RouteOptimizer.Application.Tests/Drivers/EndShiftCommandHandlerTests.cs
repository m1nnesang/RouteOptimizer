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
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly EndShiftCommandHandler _handler;

    public EndShiftCommandHandlerTests()
    {
        _handler = new EndShiftCommandHandler(_shiftRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_ShiftNotFound_ThrowsNotFoundException()
    {
        _shiftRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);

        var act = () => _handler.Handle(new EndShiftCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShiftInDifferentWarehouse_ThrowsNotFoundException()
    {
        var shift = CreateShift(Guid.NewGuid());
        _currentUser.Setup(x => x.WarehouseId).Returns(Guid.NewGuid());

        _shiftRepository
            .Setup(x => x.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var act = () => _handler.Handle(new EndShiftCommand(shift.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShiftNotStarted_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var shift = CreateShift(warehouseId);
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);

        _shiftRepository
            .Setup(x => x.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new EndShiftCommand(shift.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ActiveShift_EndsShiftAndReturnsSuccess()
    {
        var warehouseId = Guid.NewGuid();
        var shift = CreateShift(warehouseId);
        shift.Start();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);

        _shiftRepository
            .Setup(x => x.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await _handler.Handle(new EndShiftCommand(shift.Id), default);

        result.IsSuccess.Should().BeTrue();
        shift.EndedAt.Should().NotBeNull();
    }

    private static DriverShift CreateShift(Guid warehouseId) =>
        DriverShift.Create(Guid.NewGuid(), Guid.NewGuid(), warehouseId,
            DateOnly.FromDateTime(DateTime.UtcNow)).Value!;
}
