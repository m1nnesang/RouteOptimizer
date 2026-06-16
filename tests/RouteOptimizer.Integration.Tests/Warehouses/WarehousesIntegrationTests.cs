using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Warehouses.GetWarehouses;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Warehouses;

[Collection(IntegrationCollection.Name)]
public sealed class WarehousesIntegrationTests : IntegrationTestBase
{
    private const string ManagerEmail = "manager@example.com";
    private const string Password = "Password123!";

    public WarehousesIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Manager_creates_warehouse_returns_id()
    {
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var response = await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("Main Depot"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_warehouses_returns_all_warehouses()
    {
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("Depot A"));
        await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("Depot B"));

        var response = await Client.GetAsync("/api/warehouses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouses = await response.Content.ReadFromJsonAsync<List<WarehouseListItemDto>>();
        warehouses!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Dispatcher_can_get_warehouses()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync("dispatcher@example.com", Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync("dispatcher@example.com", Password);

        var response = await Client.GetAsync("/api/warehouses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_updates_warehouse_returns_no_content()
    {
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var createResponse = await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("Old Name"));
        var warehouseId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.PutAsJsonAsync($"/api/warehouses/{warehouseId}", new
        {
            Name = "New Name",
            Street = "2 Updated Road",
            City = "Hamburg",
            PostalCode = "20095",
            Country = "Germany"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Manager_deletes_warehouse_returns_no_content()
    {
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var createResponse = await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("To Delete"));
        var warehouseId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.DeleteAsync($"/api/warehouses/{warehouseId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Dispatcher_cannot_create_warehouse()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync("dispatcher@example.com", Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync("dispatcher@example.com", Password);

        var response = await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("Forbidden Depot"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static object BuildWarehouseRequest(string name) => new
    {
        Name = name,
        City = "Berlin",
        Street = "1 Test Street",
        PostalCode = "10115",
        Country = "Germany",
        Latitude = 52.5200,
        Longitude = 13.4050
    };
}
