using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class InvitationIntegrationTests : IntegrationTestBase
{
    private const string ManagerEmail = "manager@example.com";
    private const string ManagerPassword = "Password123!";

    public InvitationIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Invite_driver_accept_invitation_then_login_succeeds()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, ManagerPassword, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, ManagerPassword);

        const string driverEmail = "newdriver@example.com";
        var inviteResponse = await Client.PostAsJsonAsync("/api/drivers/invite", new
        {
            Email = driverEmail,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+12025550199",
            WarehouseId = warehouseId,
            LicenseCategory = "B"
        });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitedUser = await GetUserByEmailAsync(driverEmail);
        invitedUser.Should().NotBeNull();
        var token = await GetInvitationTokenAsync(invitedUser!.Id);

        const string newPassword = "NewPassword123!";
        var acceptResponse = await Client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            InvitationToken = token,
            Password = newPassword
        });
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = driverEmail,
            Password = newPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Accept_invitation_with_invalid_token_returns_bad_request()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            InvitationToken = "invalid-token-that-does-not-exist",
            Password = "SomePassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_invitation_twice_returns_bad_request()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(ManagerEmail, ManagerPassword, UserRole.Manager);
        await AuthenticateAsync(ManagerEmail, ManagerPassword);

        const string driverEmail = "driver2@example.com";
        await Client.PostAsJsonAsync("/api/drivers/invite", new
        {
            Email = driverEmail,
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+12025550198",
            WarehouseId = warehouseId,
            LicenseCategory = "B"
        });

        var invitedUser = await GetUserByEmailAsync(driverEmail);
        var token = await GetInvitationTokenAsync(invitedUser!.Id);

        await Client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            InvitationToken = token,
            Password = "FirstPassword123!"
        });

        var secondAccept = await Client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            InvitationToken = token,
            Password = "SecondPassword123!"
        });
        secondAccept.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Only_manager_can_invite_driver()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync("dispatcher@example.com", "Password123!", UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync("dispatcher@example.com", "Password123!");

        var response = await Client.PostAsJsonAsync("/api/drivers/invite", new
        {
            Email = "newdriver@example.com",
            FirstName = "Test",
            LastName = "Driver",
            PhoneNumber = "+12025550197",
            WarehouseId = warehouseId,
            LicenseCategory = "B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
