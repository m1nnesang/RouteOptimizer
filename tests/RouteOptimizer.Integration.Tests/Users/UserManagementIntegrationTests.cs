using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Users;

[Collection(IntegrationCollection.Name)]
public sealed class UserManagementIntegrationTests : IntegrationTestBase
{
    private const string ManagerEmail = "manager@example.com";
    private const string Password = "Password123!";

    public UserManagementIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Manager_deactivates_user_returns_no_content()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        var driver = await SeedUserAsync("driver@example.com", Password, UserRole.Driver, warehouseId);
        await AuthenticateAsync(ManagerEmail, Password);

        var response = await Client.PostAsync($"/api/users/{driver.Id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Manager_cannot_deactivate_self()
    {
        var manager = await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var response = await Client.PostAsync($"/api/users/{manager.Id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deactivating_non_existent_user_returns_not_found()
    {
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, Password);

        var response = await Client.PostAsync($"/api/users/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Non_manager_cannot_deactivate_user()
    {
        var warehouseId = await SeedWarehouseAsync();
        var driver = await SeedUserAsync("driver@example.com", Password, UserRole.Driver, warehouseId);
        await SeedUserAsync("dispatcher@example.com", Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync("dispatcher@example.com", Password);

        var response = await Client.PostAsync($"/api/users/{driver.Id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivated_user_cannot_login()
    {
        const string driverEmail = "driver@example.com";
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, Password, UserRole.Manager);
        var driver = await SeedUserAsync(driverEmail, Password, UserRole.Driver, warehouseId);
        await AuthenticateAsync(ManagerEmail, Password);

        await Client.PostAsync($"/api/users/{driver.Id}/deactivate", null);

        Client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Email = driverEmail, Password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
