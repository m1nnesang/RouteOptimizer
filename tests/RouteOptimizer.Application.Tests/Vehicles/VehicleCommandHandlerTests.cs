using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Vehicles.CreateVehicle;
using RouteOptimizer.Application.Vehicles.DeleteVehicle;
using RouteOptimizer.Application.Vehicles.GetVehicles;
using RouteOptimizer.Application.Vehicles.UpdateVehicle;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Vehicles;

public class VehicleCommandHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();

    #region CreateVehicle

    [Fact]
    public async Task CreateVehicle_WarehouseNotFound_ReturnsFailure()
    {
        _warehouseRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var handler = new CreateVehicleCommandHandler(_vehicleRepo.Object, _warehouseRepo.Object);
        var cmd = new CreateVehicleCommand(Guid.NewGuid(), "Truck", 5000m, 20m, LicenseCategory.C, [CargoType.General]);

        var result = await handler.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CreateVehicle_ValidCommand_ReturnsNewId()
    {
        var warehouseId = Guid.NewGuid();
        SetupWarehouse(warehouseId);
        _vehicleRepo.Setup(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateVehicleCommandHandler(_vehicleRepo.Object, _warehouseRepo.Object);
        var cmd = new CreateVehicleCommand(warehouseId, "Truck", 5000m, 20m, LicenseCategory.C, [CargoType.General]);

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateVehicle_InvalidWeight_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        SetupWarehouse(warehouseId);

        var handler = new CreateVehicleCommandHandler(_vehicleRepo.Object, _warehouseRepo.Object);
        var cmd = new CreateVehicleCommand(warehouseId, "Truck", -1m, 20m, LicenseCategory.C, [CargoType.General]);

        var result = await handler.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region UpdateVehicle

    [Fact]
    public async Task UpdateVehicle_VehicleNotFound_ReturnsFailure()
    {
        _vehicleRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var handler = new UpdateVehicleCommandHandler(_vehicleRepo.Object);
        var cmd = new UpdateVehicleCommand(Guid.NewGuid(), "Van", 3000m, 10m, LicenseCategory.B, [CargoType.General]);

        var result = await handler.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateVehicle_ValidCommand_UpdatesAndReturnsSuccess()
    {
        var vehicle = CreateVehicle();
        _vehicleRepo.Setup(x => x.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _vehicleRepo.Setup(x => x.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateVehicleCommandHandler(_vehicleRepo.Object);
        var cmd = new UpdateVehicleCommand(vehicle.Id, "Van", 3000m, 10m, LicenseCategory.B, [CargoType.Fragile]);

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        vehicle.Type.Should().Be("Van");
    }

    #endregion

    #region DeleteVehicle

    [Fact]
    public async Task DeleteVehicle_VehicleNotFound_ReturnsFailure()
    {
        _vehicleRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var handler = new DeleteVehicleCommandHandler(_vehicleRepo.Object);

        var result = await handler.Handle(new DeleteVehicleCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVehicle_ValidCommand_ReturnsSuccess()
    {
        var vehicle = CreateVehicle();
        _vehicleRepo.Setup(x => x.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        _vehicleRepo.Setup(x => x.DeleteAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteVehicleCommandHandler(_vehicleRepo.Object);

        var result = await handler.Handle(new DeleteVehicleCommand(vehicle.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region GetVehicles

    [Fact]
    public async Task GetVehicles_ReturnsListForWarehouse()
    {
        var warehouseId = Guid.NewGuid();
        var vehicles = new List<Vehicle> { CreateVehicle(warehouseId), CreateVehicle(warehouseId) };
        _vehicleRepo.Setup(x => x.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicles);

        var handler = new GetVehiclesQueryHandler(_vehicleRepo.Object);

        var result = await handler.Handle(new GetVehiclesQuery(warehouseId), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetVehicles_EmptyWarehouse_ReturnsEmptyList()
    {
        var warehouseId = Guid.NewGuid();
        _vehicleRepo.Setup(x => x.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetVehiclesQueryHandler(_vehicleRepo.Object);

        var result = await handler.Handle(new GetVehiclesQuery(warehouseId), default);

        result.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private void SetupWarehouse(Guid warehouseId)
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        var warehouse = Warehouse.Create("Magazyn", address, location).Value!;
        _warehouseRepo.Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
    }

    private static Vehicle CreateVehicle(Guid? warehouseId = null) =>
        Vehicle.Create(warehouseId ?? Guid.NewGuid(), "Truck",
            Weight.Create(5000m).Value!, Volume.Create(20m).Value!,
            LicenseCategory.C, [CargoType.General]).Value!;

    #endregion
}
