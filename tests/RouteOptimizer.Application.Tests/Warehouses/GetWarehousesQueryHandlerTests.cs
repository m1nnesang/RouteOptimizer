using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Warehouses.GetWarehouses;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Warehouses;

public class GetWarehousesQueryHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly GetWarehousesQueryHandler _handler;

    public GetWarehousesQueryHandlerTests()
    {
        _handler = new GetWarehousesQueryHandler(_warehouseRepository.Object);
    }

    [Fact]
    public async Task Handle_NoWarehouses_ReturnsEmptyList()
    {
        _warehouseRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetWarehousesQuery(), default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WarehousesExist_ReturnsMappedDtos()
    {
        var warehouse = CreateWarehouse();
        _warehouseRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([warehouse]);

        var result = await _handler.Handle(new GetWarehousesQuery(), default);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(warehouse.Id);
        result[0].Name.Should().Be(warehouse.Name);
        result[0].City.Should().Be(warehouse.Address.City);
    }

    private static Warehouse CreateWarehouse()
    {
        var address = Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.23, 21.01).Value!;
        return Warehouse.Create("Magazyn Testowy", address, location).Value!;
    }
}
