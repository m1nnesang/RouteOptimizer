using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using RouteOptimizer.Application.Vehicles.GetVehicles;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Vehicles;

[Collection(IntegrationCollection.Name)]
public sealed class VehiclesIntegrationTests : IntegrationTestBase
{
    private const string ManagerEmail = "manager@example.com";
    private const string Password = "Password123!";

    private static readonly JsonSerializerOptions EnumOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public VehiclesIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Manager_creates_vehicle_returns_id()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var response = await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(warehouseId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_vehicles_by_warehouse_returns_list()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(warehouseId));
        await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(warehouseId));

        var response = await Client.GetAsync($"/api/vehicles?warehouseId={warehouseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleListItemDto>>(EnumOptions);
        vehicles!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_vehicles_returns_only_vehicles_for_specified_warehouse()
    {
        var warehouseId = await SeedWarehouseAsync();
        var otherWarehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(warehouseId));
        await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(otherWarehouseId));

        var response = await Client.GetAsync($"/api/vehicles?warehouseId={warehouseId}");

        var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleListItemDto>>(EnumOptions);
        vehicles!.Should().HaveCount(1);
    }

    [Fact]
    public async Task Manager_updates_vehicle_returns_no_content()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var createResponse = await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(warehouseId));
        var vehicleId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.PutAsJsonAsync($"/api/vehicles/{vehicleId}", new
        {
            Type = "Large Truck",
            MaxWeightKg = 5000m,
            MaxVolumeM3 = 50m,
            LicenseCategory = nameof(LicenseCategory.C),
            AllowedCargoTypes = new[] { nameof(CargoType.General), nameof(CargoType.Refrigerated) }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Manager_deletes_vehicle_returns_no_content()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var vehicleId = await SeedVehicleAsync(warehouseId);

        var response = await Client.DeleteAsync($"/api/vehicles/{vehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await Client.GetAsync($"/api/vehicles?warehouseId={warehouseId}");
        var vehicles = await list.Content.ReadFromJsonAsync<List<VehicleListItemDto>>(EnumOptions);
        vehicles!.Should().BeEmpty();
    }

    [Fact]
    public async Task Dispatcher_cannot_create_vehicle()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync("dispatcher@example.com", Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync("dispatcher@example.com", Password);

        var response = await Client.PostAsJsonAsync("/api/vehicles", BuildCreateVehicleRequest(warehouseId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static object BuildCreateVehicleRequest(Guid warehouseId) => new
    {
        WarehouseId = warehouseId,
        Type = "Van",
        MaxWeightKg = 1000m,
        MaxVolumeM3 = 10m,
        LicenseCategory = nameof(LicenseCategory.B),
        AllowedCargoTypes = new[] { nameof(CargoType.General) }
    };
}
