using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Drivers.DriverShifts.StartShift;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Drivers;

public class StartShiftCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IVehicleRepository> _vehicleRepository = new();
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StartShiftCommandHandler _handler;

    public StartShiftCommandHandlerTests()
    {
        _handler = new StartShiftCommandHandler(
            _shiftRepository.Object,
            _userRepository.Object,
            _vehicleRepository.Object);
    }

    [Fact]
    public async Task Handle_DriverNotFound_ThrowsNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserIsNotDriver_ReturnsFailure()
    {
        var manager = CreateUser(UserRole.Manager);
        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DriverAlreadyHasActiveShift_ReturnsFailure()
    {
        var driver = CreateUser(UserRole.Driver);
        var existingShift = DriverShift.Create(driver.Id, Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow)).Value!;

        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(driver);
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(driver.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShift);

        var result = await _handler.Handle(ValidCommand() with { DriverId = driver.Id }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        var driver = CreateUser(UserRole.Driver);
        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(driver);
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);
        _vehicleRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_VehicleFromDifferentWarehouse_ReturnsFailure()
    {
        var driver = CreateUser(UserRole.Driver);
        var vehicle = CreateVehicle(Guid.NewGuid());

        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(driver);
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);
        _vehicleRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var result = await _handler.Handle(ValidCommand() with { WarehouseId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesShiftAndReturnsId()
    {
        var warehouseId = Guid.NewGuid();
        var driver = CreateUser(UserRole.Driver);
        var vehicle = CreateVehicle(warehouseId);

        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(driver);
        _shiftRepository
            .Setup(x => x.GetActiveShiftByDriverIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriverShift?)null);
        _vehicleRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _shiftRepository
            .Setup(x => x.AddAsync(It.IsAny<DriverShift>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            ValidCommand() with { WarehouseId = warehouseId, VehicleId = vehicle.Id },
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    private static StartShiftCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

    private static User CreateUser(UserRole role)
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        var profile = role == UserRole.Driver ? new DriverProfile(LicenseCategory.B) : null;
        var warehouseId = role == UserRole.Manager ? (Guid?)null : Guid.NewGuid();
        return User.Create("user@test.com", "Jan", "Kowalski", "hashed", phone,
            role, warehouseId, profile).Value!;
    }

    private static Vehicle CreateVehicle(Guid warehouseId)
    {
        var weight = Weight.Create(1000m).Value!;
        var volume = Volume.Create(10m).Value!;
        return Vehicle.Create(warehouseId, "Truck", weight, volume,
            LicenseCategory.B, [CargoType.General]).Value!;
    }
}
