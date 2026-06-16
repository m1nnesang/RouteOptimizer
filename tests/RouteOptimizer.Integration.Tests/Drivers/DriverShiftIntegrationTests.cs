using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Application.Drivers.GetDrivers;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Drivers;

[Collection(IntegrationCollection.Name)]
public sealed class DriverShiftIntegrationTests : IntegrationTestBase
{
    private const string DriverEmail = "driver@example.com";
    private const string ManagerEmail = "manager@example.com";
    private const string Password = "Password123!";

    public DriverShiftIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Driver_starts_shift_returns_shift_id()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        await AuthenticateAsync(DriverEmail, Password);

        var response = await Client.PostAsJsonAsync("/api/drivers/shifts/start", new
        {
            VehicleId = vehicleId,
            WarehouseId = warehouseId,
            ShiftDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shiftId = await response.Content.ReadFromJsonAsync<Guid>();
        shiftId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Driver_ends_shift_returns_no_content()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        await AuthenticateAsync(DriverEmail, Password);

        var startResponse = await Client.PostAsJsonAsync("/api/drivers/shifts/start", new
        {
            VehicleId = vehicleId,
            WarehouseId = warehouseId,
            ShiftDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        var shiftId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        var endResponse = await Client.PostAsync($"/api/drivers/shifts/{shiftId}/end", null);

        endResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Driver_cannot_start_shift_with_vehicle_from_another_warehouse()
    {
        var warehouseId = await SeedWarehouseAsync();
        var otherWarehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(otherWarehouseId);
        await AuthenticateAsync(DriverEmail, Password);

        var response = await Client.PostAsJsonAsync("/api/drivers/shifts/start", new
        {
            VehicleId = vehicleId,
            WarehouseId = warehouseId,
            ShiftDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_drivers_returns_only_driver_role_users()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        await SeedUserAsync("driver2@example.com", Password, UserRole.Driver, warehouseId);
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var response = await Client.GetAsync("/api/drivers?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<DriverListItemDto>>();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(d => d.LicenseCategory != null);
    }

    [Fact]
    public async Task Only_driver_can_start_shift()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync("dispatcher@example.com", Password, UserRole.Dispatcher, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        await AuthenticateAsync("dispatcher@example.com", Password);

        var response = await Client.PostAsJsonAsync("/api/drivers/shifts/start", new
        {
            VehicleId = vehicleId,
            WarehouseId = warehouseId,
            ShiftDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
