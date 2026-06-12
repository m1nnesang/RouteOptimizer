using FluentAssertions;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.Entities;

public class WarehouseTests
{
    private static Address ValidAddress =>
        Address.Create("ul. Testowa 1", "Warszawa", "00-001", "Poland").Value!;

    private static GeoCoordinate ValidLocation =>
        GeoCoordinate.Create(52.23, 21.01).Value!;

    [Fact]
    public void Create_ValidParameters_ReturnsSuccess()
    {
        var result = Warehouse.Create("Magazyn Główny", ValidAddress, ValidLocation);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Magazyn Główny");
        result.Value.Address.Should().Be(ValidAddress);
        result.Value.Location.Should().Be(ValidLocation);
    }

    [Fact]
    public void Create_EmptyName_ReturnsFailure()
    {
        var result = Warehouse.Create("   ", ValidAddress, ValidLocation);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Update_ValidParameters_UpdatesAllFields()
    {
        var warehouse = Warehouse.Create("Stara Nazwa", ValidAddress, ValidLocation).Value!;
        var newAddress = Address.Create("ul. Nowa 5", "Kraków", "30-001", "Poland").Value!;
        var newLocation = GeoCoordinate.Create(50.06, 19.94).Value!;

        var result = warehouse.Update("Nowa Nazwa", newAddress, newLocation);

        result.IsSuccess.Should().BeTrue();
        warehouse.Name.Should().Be("Nowa Nazwa");
        warehouse.Address.Should().Be(newAddress);
        warehouse.Location.Should().Be(newLocation);
    }

    [Fact]
    public void Update_EmptyName_ReturnsFailure()
    {
        var warehouse = Warehouse.Create("Magazyn", ValidAddress, ValidLocation).Value!;

        var result = warehouse.Update("", ValidAddress, ValidLocation);

        result.IsFailure.Should().BeTrue();
        warehouse.Name.Should().Be("Magazyn");
    }
}
