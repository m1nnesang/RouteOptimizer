using FluentAssertions;
using RouteOptimizer.Dispatcher.Wpf.Services;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.Services;

public class TokenStorageTests
{
    [Fact]
    public void Save_StoresBothTokens()
    {
        var storage = new TokenStorage();

        storage.Save("access", "refresh");

        storage.AccessToken.Should().Be("access");
        storage.RefreshToken.Should().Be("refresh");
    }

    [Fact]
    public void Clear_ResetsBothTokens()
    {
        var storage = new TokenStorage();
        storage.Save("access", "refresh");

        storage.Clear();

        storage.AccessToken.Should().BeNull();
        storage.RefreshToken.Should().BeNull();
    }
}
