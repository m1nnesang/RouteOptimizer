namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class TokenStorage
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public void Save(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
    }
}
