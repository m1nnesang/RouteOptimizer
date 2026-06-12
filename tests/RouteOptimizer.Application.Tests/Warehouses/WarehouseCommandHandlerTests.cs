using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Warehouses.CreateWarehouse;
using RouteOptimizer.Application.Warehouses.DeleteWarehouse;
using RouteOptimizer.Application.Warehouses.UpdateWarehouse;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Warehouses;

public class WarehouseCommandHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IGeocodingService> _geocoding = new();

    private static GeoCoordinate ValidLocation => GeoCoordinate.Create(52.23, 21.01).Value!;

    #region CreateWarehouse

    [Fact]
    public async Task CreateWarehouse_ValidCommand_ReturnsNewId()
    {
        _geocoding.Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(ValidLocation));
        _warehouseRepo.Setup(x => x.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateWarehouseCommandHandler(_warehouseRepo.Object, _geocoding.Object);
        var cmd = new CreateWarehouseCommand("Magazyn Główny", "Warszawa", "ul. Testowa 1", "00-001", "Poland", 52.23, 21.01);

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateWarehouse_GeocodingFails_ReturnsFailure()
    {
        _geocoding.Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Failure("Geocoding service unavailable"));

        var handler = new CreateWarehouseCommandHandler(_warehouseRepo.Object, _geocoding.Object);
        var cmd = new CreateWarehouseCommand("Magazyn", "Warszawa", "ul. Testowa 1", "00-001", "Poland", 52.23, 21.01);

        var result = await handler.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWarehouse_InvalidAddress_ReturnsFailure()
    {
        var handler = new CreateWarehouseCommandHandler(_warehouseRepo.Object, _geocoding.Object);
        var cmd = new CreateWarehouseCommand("Magazyn", "", "", "", "", 0, 0);

        var result = await handler.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
        _geocoding.Verify(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateWarehouse

    [Fact]
    public async Task UpdateWarehouse_WarehouseNotFound_ReturnsFailure()
    {
        _warehouseRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var handler = new UpdateWarehouseCommandHandler(_warehouseRepo.Object, _geocoding.Object);
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), "Nowa Nazwa", "ul. Nowa 1", "Kraków", "30-001", "Poland");

        var result = await handler.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWarehouse_ValidCommand_UpdatesAndReturnsSuccess()
    {
        var warehouse = CreateWarehouse();
        _warehouseRepo.Setup(x => x.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _geocoding.Setup(x => x.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GeoCoordinate>.Success(ValidLocation));
        _warehouseRepo.Setup(x => x.UpdateAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateWarehouseCommandHandler(_warehouseRepo.Object, _geocoding.Object);
        var cmd = new UpdateWarehouseCommand(warehouse.Id, "Nowa Nazwa", "ul. Nowa 1", "Kraków", "30-001", "Poland");

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        warehouse.Name.Should().Be("Nowa Nazwa");
    }

    #endregion

    #region DeleteWarehouse

    [Fact]
    public async Task DeleteWarehouse_WarehouseNotFound_ReturnsFailure()
    {
        _warehouseRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var handler = new DeleteWarehouseCommandHandler(_warehouseRepo.Object);

        var result = await handler.Handle(new DeleteWarehouseCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteWarehouse_ValidCommand_ReturnsSuccess()
    {
        var warehouse = CreateWarehouse();
        _warehouseRepo.Setup(x => x.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _warehouseRepo.Setup(x => x.DeleteAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteWarehouseCommandHandler(_warehouseRepo.Object);

        var result = await handler.Handle(new DeleteWarehouseCommand(warehouse.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private static Warehouse CreateWarehouse()
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        return Warehouse.Create("Magazyn", address, ValidLocation).Value!;
    }

    #endregion
}
