using Blazored.LocalStorage;

namespace RouteOptimizer.Driver.Pwa.Services;

public sealed class LocalStorageTokenStore : ITokenStore
{
    private const string AccessTokenKey = "ro_access_token";
    private const string RefreshTokenKey = "ro_refresh_token";

    private readonly ILocalStorageService _storage;

    public LocalStorageTokenStore(ILocalStorageService storage) => _storage = storage;

    public async Task<string?> GetAccessTokenAsync() =>
        await _storage.GetItemAsync<string>(AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync() =>
        await _storage.GetItemAsync<string>(RefreshTokenKey);

    public async Task SaveAsync(string accessToken, string refreshToken)
    {
        await _storage.SetItemAsync(AccessTokenKey, accessToken);
        await _storage.SetItemAsync(RefreshTokenKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await _storage.RemoveItemAsync(AccessTokenKey);
        await _storage.RemoveItemAsync(RefreshTokenKey);
    }
}
