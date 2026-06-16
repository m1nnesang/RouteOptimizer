using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Routes.HandoverRoute;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Routes;

[Collection(IntegrationCollection.Name)]
public sealed class HandoverRouteIntegrationTests : IntegrationTestBase
{
    private const string DispatcherEmail = "dispatcher@example.com";
    private const string DriverEmail = "driver@example.com";
    private const string Password = "Password123!";

    public HandoverRouteIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handover_transfer_all_creates_new_route_for_target_shift()
    {
        var (routeId, _, targetShiftId) = await SetupInterruptedRouteWithTargetShiftAsync();

        await AuthenticateAsync(DispatcherEmail, Password);
        var response = await Client.PostAsJsonAsync($"/api/routes/{routeId}/handover", new
        {
            Type = nameof(HandoverType.TransferAll),
            TargetShiftId = targetShiftId,
            Assignments = (object?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var routesResponse = await Client.GetAsync("/api/routes");
        var routesJson = await routesResponse.Content.ReadFromJsonAsync<PagedRouteResult>();
        routesJson!.Items.Should().HaveCount(2);

        var newRoute = routesJson.Items.Single(r => r.Id != routeId);
        newRoute.AssignedShiftId.Should().Be(targetShiftId);
        newRoute.DriverName.Should().Be("Test User");
        newRoute.WarehouseName.Should().Be("Test Warehouse");
        newRoute.Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        newRoute.StopsCount.Should().Be(2);
    }

    [Fact]
    public async Task Handover_return_to_pool_leaves_original_route_interrupted()
    {
        var (routeId, _, _) = await SetupInterruptedRouteWithTargetShiftAsync();

        await AuthenticateAsync(DispatcherEmail, Password);
        var response = await Client.PostAsJsonAsync($"/api/routes/{routeId}/handover", new
        {
            Type = nameof(HandoverType.ReturnToPool),
            TargetShiftId = (Guid?)null,
            Assignments = (object?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Status.Should().Be(nameof(RouteStatus.Interrupted));
    }

    [Fact]
    public async Task Handover_distribute_creates_one_route_per_shift()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);

        var driver1 = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId1 = await SeedVehicleAsync(warehouseId);
        var shift1 = await SeedDriverShiftAsync(driver1.Id, vehicleId1, warehouseId);

        var driver2 = await SeedUserAsync("driver2@example.com", Password, UserRole.Driver, warehouseId);
        var vehicleId2 = await SeedVehicleAsync(warehouseId);
        var shift2 = await SeedDriverShiftAsync(driver2.Id, vehicleId2, warehouseId);

        await AuthenticateAsync(DispatcherEmail, Password);
        var (routeId, stopIds) = await CreateAndOptimizeRouteAsync(warehouseId);
        await Client.PostAsJsonAsync($"/api/routes/{routeId}/assign", new { ShiftId = shift1 });

        await AuthenticateAsync(DriverEmail, Password);
        await Client.PostAsync($"/api/routes/{routeId}/start", null);

        await AuthenticateAsync(DispatcherEmail, Password);
        var handoverResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/handover", new
        {
            Type = nameof(HandoverType.Distribute),
            TargetShiftId = (Guid?)null,
            Assignments = new[]
            {
                new { ShiftId = shift1, StopIds = new[] { stopIds[0] } },
                new { ShiftId = shift2, StopIds = new[] { stopIds[1] } }
            }
        });

        handoverResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var routesResponse = await Client.GetAsync("/api/routes");
        var routesJson = await routesResponse.Content.ReadFromJsonAsync<PagedRouteResult>();
        routesJson!.Items.Count(r => r.Id != routeId).Should().Be(2);
    }

    [Fact]
    public async Task Handover_transfer_all_to_shift_from_different_warehouse_fails()
    {
        var warehouseId = await SeedWarehouseAsync();
        var otherWarehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);

        var driver = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        var shiftId = await SeedDriverShiftAsync(driver.Id, vehicleId, warehouseId);

        var driver2 = await SeedUserAsync("driver2@example.com", Password, UserRole.Driver, otherWarehouseId);
        var vehicleId2 = await SeedVehicleAsync(otherWarehouseId);
        var otherShiftId = await SeedDriverShiftAsync(driver2.Id, vehicleId2, otherWarehouseId);

        await AuthenticateAsync(DispatcherEmail, Password);
        var (routeId, _) = await CreateAndOptimizeRouteAsync(warehouseId);
        await Client.PostAsJsonAsync($"/api/routes/{routeId}/assign", new { ShiftId = shiftId });

        await AuthenticateAsync(DriverEmail, Password);
        await Client.PostAsync($"/api/routes/{routeId}/start", null);

        await AuthenticateAsync(DispatcherEmail, Password);
        var response = await Client.PostAsJsonAsync($"/api/routes/{routeId}/handover", new
        {
            Type = nameof(HandoverType.TransferAll),
            TargetShiftId = otherShiftId,
            Assignments = (object?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid RouteId, IReadOnlyList<Guid> StopIds, Guid TargetShiftId)> SetupInterruptedRouteWithTargetShiftAsync()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);

        var driver = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        var shiftId = await SeedDriverShiftAsync(driver.Id, vehicleId, warehouseId);

        var driver2 = await SeedUserAsync("driver2@example.com", Password, UserRole.Driver, warehouseId);
        var vehicleId2 = await SeedVehicleAsync(warehouseId);
        var targetShiftId = await SeedDriverShiftAsync(driver2.Id, vehicleId2, warehouseId);

        await AuthenticateAsync(DispatcherEmail, Password);
        var (routeId, stopIds) = await CreateAndOptimizeRouteAsync(warehouseId);
        await Client.PostAsJsonAsync($"/api/routes/{routeId}/assign", new { ShiftId = shiftId });

        await AuthenticateAsync(DriverEmail, Password);
        await Client.PostAsync($"/api/routes/{routeId}/start", null);

        await AuthenticateAsync(DispatcherEmail, Password);
        await Client.PostAsync($"/api/routes/{routeId}/interrupt", null);

        return (routeId, stopIds, targetShiftId);
    }

    private async Task<(Guid RouteId, IReadOnlyList<Guid> StopIds)> CreateAndOptimizeRouteAsync(Guid warehouseId)
    {
        var order1 = await CreateIndividualOrderAsync(warehouseId);
        var order2 = await CreateIndividualOrderAsync(warehouseId);

        var createResponse = await Client.PostAsJsonAsync("/api/routes", new
        {
            WarehouseId = warehouseId,
            OrderIds = new[] { order1, order2 }
        });
        var routeId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var optimizeResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/optimize", new
        {
            RouteDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        var optimizeResult = await optimizeResponse.Content.ReadFromJsonAsync<OptimizeResult>();

        return (routeId, optimizeResult!.OrderedStopIds);
    }

    private async Task<Guid> CreateIndividualOrderAsync(Guid warehouseId)
    {
        var response = await Client.PostAsJsonAsync("/api/orders/individual", new
        {
            WarehouseId = warehouseId,
            Street = "1 Market Street",
            City = "Berlin",
            Postcode = "10115",
            Country = "Germany",
            CustomerName = "John Doe",
            PhoneNumber = "+12025550123",
            Weight = 1.0m,
            Volume = 1.0m,
            CargoType = nameof(CargoType.General),
            AllowLeaveAtDoor = false,
            Notes = (string?)null,
            Start = (TimeOnly?)null,
            End = (TimeOnly?)null,
            WindowStrictness = (string?)null
        });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<RouteDetailDto> GetRouteAsync(Guid routeId)
    {
        var response = await Client.GetAsync($"/api/routes/{routeId}");
        return (await response.Content.ReadFromJsonAsync<RouteDetailDto>())!;
    }

    private sealed record OptimizeResult(Guid RouteId, IReadOnlyList<Guid> OrderedStopIds);
    private sealed record RouteDetailDto(Guid Id, Guid WarehouseId, Guid? AssignedShiftId, string Status);
    private sealed record RouteListItem(Guid Id, Guid WarehouseId, string Status, int StopsCount,
        Guid? AssignedShiftId, DateOnly? Date, string? DriverName, string WarehouseName);
    private sealed record PagedRouteResult(IReadOnlyList<RouteListItem> Items, int TotalCount);
}
