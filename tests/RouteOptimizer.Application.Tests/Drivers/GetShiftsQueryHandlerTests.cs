using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Drivers.GetShifts;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Drivers;

public class GetShiftsQueryHandlerTests
{
    private readonly Mock<IDriverShiftRepository> _shiftRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IVehicleRepository> _vehicleRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetShiftsQueryHandler _handler;

    public GetShiftsQueryHandlerTests()
    {
        _handler = new GetShiftsQueryHandler(
            _shiftRepository.Object,
            _userRepository.Object,
            _vehicleRepository.Object,
            _warehouseRepository.Object,
            _currentUser.Object);

        _userRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _vehicleRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehicle>());
        _warehouseRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse>());
    }

    [Fact]
    public async Task Handle_NoShifts_ReturnsEmptyList()
    {
        _shiftRepository
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DriverShift>(), 0));

        var result = await _handler.Handle(new GetShiftsQuery(null), default);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShiftsExist_MapsDriverVehicleAndWarehouseNames()
    {
        var driver = CreateDriver();
        var warehouse = CreateWarehouse();
        var vehicle = CreateVehicle(warehouse.Id);
        var shift = DriverShift.Create(driver.Id, vehicle.Id, warehouse.Id, Today()).Value!;

        _shiftRepository
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DriverShift> { shift }, 1));
        _userRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { driver });
        _vehicleRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehicle> { vehicle });
        _warehouseRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { warehouse });

        var result = await _handler.Handle(new GetShiftsQuery(null), default);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.DriverName.Should().Be("Jan Kowalski");
        item.VehicleType.Should().Be("Truck");
        item.WarehouseName.Should().Be("Magazyn");
    }

    [Fact]
    public async Task Handle_ScopesQueryToCurrentUserWarehouseAndDate()
    {
        var warehouseId = Guid.NewGuid();
        var date = Today();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        _shiftRepository
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DriverShift>(), 0));

        await _handler.Handle(new GetShiftsQuery(date), default);

        _shiftRepository.Verify(
            x => x.GetAllAsync(warehouseId, date, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static User CreateDriver()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return User.Create("driver@test.com", "Jan", "Kowalski", "hashed", phone,
            UserRole.Driver, Guid.NewGuid(), new DriverProfile(LicenseCategory.B)).Value!;
    }

    private static Vehicle CreateVehicle(Guid warehouseId) =>
        Vehicle.Create(warehouseId, "Truck",
            Weight.Create(5000m).Value!, Volume.Create(20m).Value!,
            LicenseCategory.C, [CargoType.General]).Value!;

    private static Warehouse CreateWarehouse()
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        return Warehouse.Create("Magazyn", address, location).Value!;
    }
}
