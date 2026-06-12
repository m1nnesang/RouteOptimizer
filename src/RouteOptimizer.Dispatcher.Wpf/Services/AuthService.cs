using System.Net.Http;
using System.Text;
using System.Text.Json;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorage _tokenStorage;
    public AuthService(HttpClient httpClient, TokenStorage tokenStorage) => (_httpClient, _tokenStorage) = (httpClient, tokenStorage);

    public async Task<bool> LoginAsync(string email, string password , CancellationToken ct)
    {
        var body = new { Email = email, Password = password};
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/auth/login", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);

        var tokenDto = JsonSerializer.Deserialize<AuthTokenResponse>(jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _tokenStorage.Save(tokenDto!.AccessToken, tokenDto.RefreshToken);

        return true;
    }

    public void Logout()
    {
        _tokenStorage.Clear();
    }
}
