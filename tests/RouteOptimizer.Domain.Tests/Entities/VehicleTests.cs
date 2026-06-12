using FluentAssertions;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests.Entities;

public class VehicleTests
{
    private static Weight ValidWeight => Weight.Create(5000m).Value!;
    private static Volume ValidVolume => Volume.Create(20m).Value!;

    private static Vehicle CreateVehicle() =>
        Vehicle.Create(Guid.NewGuid(), "Truck", ValidWeight, ValidVolume,
            LicenseCategory.C, [CargoType.General]).Value!;

    [Fact]
    public void Create_ValidParameters_ReturnsSuccess()
    {
        var warehouseId = Guid.NewGuid();

        var result = Vehicle.Create(warehouseId, "Van", ValidWeight, ValidVolume,
            LicenseCategory.B, [CargoType.General]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WarehouseId.Should().Be(warehouseId);
        result.Value.Type.Should().Be("Van");
    }

    [Fact]
    public void Create_EmptyWarehouseId_ReturnsFailure()
    {
        var result = Vehicle.Create(Guid.Empty, "Van", ValidWeight, ValidVolume,
            LicenseCategory.B, [CargoType.General]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyType_ReturnsFailure()
    {
        var result = Vehicle.Create(Guid.NewGuid(), "   ", ValidWeight, ValidVolume,
            LicenseCategory.B, [CargoType.General]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyCargoTypes_ReturnsFailure()
    {
        var result = Vehicle.Create(Guid.NewGuid(), "Van", ValidWeight, ValidVolume,
            LicenseCategory.B, []);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Update_ValidParameters_UpdatesAllFields()
    {
        var vehicle = CreateVehicle();
        var newWeight = Weight.Create(3000m).Value!;
        var newVolume = Volume.Create(10m).Value!;

        var result = vehicle.Update("Van", newWeight, newVolume, LicenseCategory.B, [CargoType.Fragile]);

        result.IsSuccess.Should().BeTrue();
        vehicle.Type.Should().Be("Van");
        vehicle.MaxWeightKg.Should().Be(newWeight);
        vehicle.MaxVolumeM3.Should().Be(newVolume);
        vehicle.LicenseCategory.Should().Be(LicenseCategory.B);
        vehicle.AllowedCargoTypes.Should().Contain(CargoType.Fragile);
    }

    [Fact]
    public void Update_EmptyType_ReturnsFailure()
    {
        var vehicle = CreateVehicle();

        var result = vehicle.Update("", ValidWeight, ValidVolume, LicenseCategory.B, [CargoType.General]);

        result.IsFailure.Should().BeTrue();
        vehicle.Type.Should().Be("Truck");
    }

    [Fact]
    public void Update_EmptyCargoTypes_ReturnsFailure()
    {
        var vehicle = CreateVehicle();

        var result = vehicle.Update("Van", ValidWeight, ValidVolume, LicenseCategory.B, []);

        result.IsFailure.Should().BeTrue();
    }
}
