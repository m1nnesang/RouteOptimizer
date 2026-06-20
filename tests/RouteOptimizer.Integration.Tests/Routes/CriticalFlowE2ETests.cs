using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Routes.HandoverRoute;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Routes;

[Collection(IntegrationCollection.Name)]
public sealed class CriticalFlowE2ETests : IntegrationTestBase
{
    private const string DispatcherEmail = "dispatcher@example.com";
    private const string Password = "Password123!";

    public CriticalFlowE2ETests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Login_create_order_optimize_route_assign_shift_then_handover()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);

        var driver = await SeedUserAsync("driver@example.com", Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        var shiftId = await SeedDriverShiftAsync(driver.Id, vehicleId, warehouseId);

        var targetDriver = await SeedUserAsync("target-driver@example.com", Password, UserRole.Driver, warehouseId);
        var targetVehicleId = await SeedVehicleAsync(warehouseId);
        var targetShiftId = await SeedDriverShiftAsync(targetDriver.Id, targetVehicleId, warehouseId);

        // 1. Login
        await AuthenticateAsync(DispatcherEmail, Password);

        // 2. Create orders
        var firstOrderId = await CreateIndividualOrderAsync(warehouseId, "1 Market Street");
        var secondOrderId = await CreateIndividualOrderAsync(warehouseId, "2 Other Avenue");

        // 3. Create route -> Draft
        var routeId = await CreateRouteAsync(firstOrderId, secondOrderId);
        (await GetRouteAsync(routeId)).Status.Should().Be(nameof(RouteStatus.Draft));

        // 4. Optimize -> Optimized
        var orderedStopIds = await OptimizeRouteAsync(routeId);
        orderedStopIds.Should().HaveCount(2);
        (await GetRouteAsync(routeId)).Status.Should().Be(nameof(RouteStatus.Optimized));

        // 5. Assign shift -> Assigned
        var assignResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/assign", new { ShiftId = shiftId });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetRouteAsync(routeId)).Status.Should().Be(nameof(RouteStatus.Assigned));

        // 6. Handover (transfer all) -> original interrupted, new route created for target shift
        var handoverResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/handover", new
        {
            Type = nameof(HandoverType.TransferAll),
            TargetShiftId = targetShiftId,
            Assignments = (object?)null
        });
        handoverResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetRouteAsync(routeId)).Status.Should().Be(nameof(RouteStatus.Interrupted));

        var routes = (await Client.GetFromJsonAsync<PagedRouteResult>("/api/routes"))!;
        routes.Items.Should().HaveCount(2);

        var newRoute = routes.Items.Single(r => r.Id != routeId);
        newRoute.AssignedShiftId.Should().Be(targetShiftId);
        newRoute.StopsCount.Should().Be(2);
    }

    private async Task<Guid> CreateRouteAsync(params Guid[] orderIds)
    {
        var response = await Client.PostAsJsonAsync("/api/routes", new
        {
            OrderIds = orderIds,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<IReadOnlyList<Guid>> OptimizeRouteAsync(Guid routeId)
    {
        var response = await Client.PostAsJsonAsync($"/api/routes/{routeId}/optimize", new
        {
            RouteDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OptimizeResult>();
        return result!.OrderedStopIds;
    }

    private async Task<RouteDetailDto> GetRouteAsync(Guid routeId)
    {
        var response = await Client.GetAsync($"/api/routes/{routeId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<RouteDetailDto>())!;
    }

    private async Task<Guid> CreateIndividualOrderAsync(Guid warehouseId, string street)
    {
        var response = await Client.PostAsJsonAsync("/api/orders/individual", new
        {
            WarehouseId = warehouseId,
            Street = street,
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private sealed record OptimizeResult(Guid RouteId, IReadOnlyList<Guid> OrderedStopIds);

    private sealed record RouteDetailDto(Guid Id, Guid WarehouseId, Guid? AssignedShiftId, string Status);

    private sealed record RouteListItem(Guid Id, Guid WarehouseId, string Status, int StopsCount,
        Guid? AssignedShiftId, DateOnly? Date, string? DriverName, string WarehouseName);

    private sealed record PagedRouteResult(IReadOnlyList<RouteListItem> Items, int TotalCount);
}
