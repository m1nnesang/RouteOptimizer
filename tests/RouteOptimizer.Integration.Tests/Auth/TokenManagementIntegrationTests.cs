using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Auth;

namespace RouteOptimizer.Integration.Tests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class TokenManagementIntegrationTests : IntegrationTestBase
{
    private const string Email = "user@example.com";
    private const string Password = "Password123!";

    public TokenManagementIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Refresh_with_valid_token_returns_new_token_pair()
    {
        await SeedUserAsync(Email, Password);
        var tokens = await LoginAsync(Email, Password);

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { Token = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        newTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        newTokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        newTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_with_invalid_token_returns_unauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { Token = "not-a-real-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_after_revoke_returns_unauthorized()
    {
        await SeedUserAsync(Email, Password);
        var tokens = await LoginAsync(Email, Password);

        await AuthenticateAsync(Email, Password);
        var revokeResponse = await Client.PostAsJsonAsync("/api/auth/revoke", new { Token = tokens.RefreshToken });
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new { Token = tokens.RefreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_with_valid_token_returns_no_content()
    {
        await SeedUserAsync(Email, Password);
        var tokens = await LoginAsync(Email, Password);
        await AuthenticateAsync(Email, Password);

        var response = await Client.PostAsJsonAsync("/api/auth/revoke", new { Token = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_with_invalid_token_returns_bad_request()
    {
        await SeedUserAsync(Email, Password);
        await AuthenticateAsync(Email, Password);

        var response = await Client.PostAsJsonAsync("/api/auth/revoke", new { Token = "invalid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_token_is_single_use()
    {
        await SeedUserAsync(Email, Password);
        var tokens = await LoginAsync(Email, Password);

        var firstRefresh = await Client.PostAsJsonAsync("/api/auth/refresh", new { Token = tokens.RefreshToken });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondRefresh = await Client.PostAsJsonAsync("/api/auth/refresh", new { Token = tokens.RefreshToken });
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
