using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Auth;
using RouteOptimizer.Application.Auth.Login;

namespace RouteOptimizer.Integration.Tests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class LoginIntegrationTests : IntegrationTestBase
{
    public LoginIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Login_with_unknown_user_returns_unauthorized()
    {
        var command = new LoginCommand("missing@example.com", "Password123");

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        const string email = "manager@example.com";
        const string password = "Password123!";
        await SeedUserAsync(email, password);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadFromJsonAsync<AuthTokenDto>();

        token.Should().NotBeNull();

        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }
}
