using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Routes;

[Collection(IntegrationCollection.Name)]
public sealed class RouteLifecycleIntegrationTests : IntegrationTestBase
{
    private const string DispatcherEmail = "dispatcher@example.com";
    private const string DriverEmail = "driver@example.com";
    private const string Password = "Password123!";

    public RouteLifecycleIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Dispatcher_creates_orders_then_route_then_optimizes()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var (_, orderedStopIds) = await CreateAndOptimizeRouteAsync(warehouseId);

        orderedStopIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task Driver_assigned_route_completes_every_stop_then_route()
    {
        var (routeId, orderedStopIds) = await StartAssignedRouteAsync();

        foreach (var stopId in orderedStopIds)
        {
            var completeStopResponse = await Client.PostAsJsonAsync(
                $"/api/routes/{routeId}/stops/{stopId}/complete", new { IsPartial = false });
            completeStopResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var completeRouteResponse = await Client.PostAsync($"/api/routes/{routeId}/complete", null);
        completeRouteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Status.Should().Be(nameof(RouteStatus.Completed));
        route.Stops.Should().OnlyContain(s => s.Status == nameof(StopStatus.Completed));
    }

    [Fact]
    public async Task Assigning_route_to_shift_from_another_warehouse_fails()
    {
        var warehouseId = await SeedWarehouseAsync();
        var otherWarehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        var driver = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, otherWarehouseId);
        var vehicleId = await SeedVehicleAsync(otherWarehouseId);
        var shiftId = await SeedDriverShiftAsync(driver.Id, vehicleId, otherWarehouseId);

        await AuthenticateAsync(DispatcherEmail, Password);
        var (routeId, _) = await CreateAndOptimizeRouteAsync(warehouseId);

        var assignResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/assign", new { ShiftId = shiftId });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Starting_an_unassigned_route_fails()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);

        await AuthenticateAsync(DispatcherEmail, Password);
        var (routeId, _) = await CreateAndOptimizeRouteAsync(warehouseId);

        await AuthenticateAsync(DriverEmail, Password);
        var startResponse = await Client.PostAsync($"/api/routes/{routeId}/start", null);

        startResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Completing_a_route_with_incomplete_stops_fails()
    {
        var (routeId, _) = await StartAssignedRouteAsync();

        var completeRouteResponse = await Client.PostAsync($"/api/routes/{routeId}/complete", null);

        completeRouteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Driver_skips_then_resumes_the_current_stop()
    {
        var (routeId, orderedStopIds) = await StartAssignedRouteAsync();
        var firstStopId = orderedStopIds[0];

        var skipResponse = await Client.PostAsync($"/api/routes/{routeId}/stops/{firstStopId}/skip", null);
        skipResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterSkip = await GetRouteAsync(routeId);
        afterSkip.Stops.Single(s => s.Id == firstStopId).Status.Should().Be(nameof(StopStatus.Skipped));

        var resumeResponse = await Client.PatchAsync($"/api/routes/{routeId}/stops/{firstStopId}/resume", null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResume = await GetRouteAsync(routeId);
        afterResume.Stops.Single(s => s.Id == firstStopId).Status.Should().Be(nameof(StopStatus.InProgress));
    }

    [Fact]
    public async Task Driver_fails_the_current_stop_delivery()
    {
        var (routeId, orderedStopIds) = await StartAssignedRouteAsync();
        var firstStopId = orderedStopIds[0];

        var failResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/stops/{firstStopId}/fail", new
        {
            Latitude = 52.5200,
            Longitude = 13.4050,
            FailureReason = nameof(FailureReason.CustomerNotAtHome),
            Notes = (string?)null,
            PhotoKey = (string?)null
        });
        failResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Stops.Single(s => s.Id == firstStopId).Status.Should().Be(nameof(StopStatus.Failed));
    }

    [Fact]
    public async Task Driver_partially_completes_the_current_stop()
    {
        var (routeId, orderedStopIds) = await StartAssignedRouteAsync();
        var firstStopId = orderedStopIds[0];

        var completeResponse = await Client.PostAsJsonAsync(
            $"/api/routes/{routeId}/stops/{firstStopId}/complete", new { IsPartial = true });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Stops.Single(s => s.Id == firstStopId).Status.Should().Be(nameof(StopStatus.PartiallyCompleted));
    }

    [Fact]
    public async Task Dispatcher_interrupts_an_in_progress_route()
    {
        var (routeId, _) = await StartAssignedRouteAsync();

        await AuthenticateAsync(DispatcherEmail, Password);
        var interruptResponse = await Client.PostAsync($"/api/routes/{routeId}/interrupt", null);
        interruptResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Status.Should().Be(nameof(RouteStatus.Interrupted));
    }

    [Fact]
    public async Task Dispatcher_cancels_an_optimized_route()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var (routeId, _) = await CreateAndOptimizeRouteAsync(warehouseId);

        var cancelResponse = await Client.PostAsync($"/api/routes/{routeId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Status.Should().Be(nameof(RouteStatus.Cancelled));
    }

    [Fact]
    public async Task Dispatcher_inserts_an_urgent_order_into_an_in_progress_route()
    {
        var (routeId, orderedStopIds) = await StartAssignedRouteAsync();
        var warehouseId = (await GetRouteAsync(routeId)).WarehouseId;

        await AuthenticateAsync(DispatcherEmail, Password);
        var urgentOrderId = await CreateIndividualOrderAsync(warehouseId);

        var insertResponse = await Client.PostAsJsonAsync(
            $"/api/routes/{routeId}/urgent-order", new { OrderId = urgentOrderId });
        insertResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var route = await GetRouteAsync(routeId);
        route.Stops.Should().HaveCount(orderedStopIds.Count + 1);
    }

    private async Task<(Guid RouteId, IReadOnlyList<Guid> StopIds)> StartAssignedRouteAsync()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        var driver = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var vehicleId = await SeedVehicleAsync(warehouseId);
        var shiftId = await SeedDriverShiftAsync(driver.Id, vehicleId, warehouseId);

        await AuthenticateAsync(DispatcherEmail, Password);
        var (routeId, stopIds) = await CreateAndOptimizeRouteAsync(warehouseId);

        var assignResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/assign", new { ShiftId = shiftId });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AuthenticateAsync(DriverEmail, Password);
        var startResponse = await Client.PostAsync($"/api/routes/{routeId}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return (routeId, stopIds);
    }

    private async Task<(Guid RouteId, IReadOnlyList<Guid> StopIds)> CreateAndOptimizeRouteAsync(Guid warehouseId)
    {
        var firstOrderId = await CreateIndividualOrderAsync(warehouseId, "1 Market Street");
        var secondOrderId = await CreateIndividualOrderAsync(warehouseId, "2 Other Avenue");

        var createRouteResponse = await Client.PostAsJsonAsync("/api/routes", new
        {
            OrderIds = new[] { firstOrderId, secondOrderId },
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        createRouteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var routeId = await createRouteResponse.Content.ReadFromJsonAsync<Guid>();

        var optimizeResponse = await Client.PostAsJsonAsync($"/api/routes/{routeId}/optimize", new
        {
            RouteDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        optimizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var optimizeResult = await optimizeResponse.Content.ReadFromJsonAsync<OptimizeResult>();

        return (routeId, optimizeResult!.OrderedStopIds);
    }

    private async Task<RouteDto> GetRouteAsync(Guid routeId)
    {
        var response = await Client.GetAsync($"/api/routes/{routeId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<RouteDto>())!;
    }

    private async Task<Guid> CreateIndividualOrderAsync(Guid warehouseId, string street = "1 Market Street")
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

    private sealed record RouteDto(Guid Id, Guid WarehouseId, Guid? AssignedShiftId, string Status,
        IReadOnlyList<StopDto> Stops);

    private sealed record StopDto(Guid Id, int Sequence, string City, string Street, double Latitude,
        double Longitude, string Status, IReadOnlyList<Guid> OrderIds);
}
