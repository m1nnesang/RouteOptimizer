using Microsoft.Extensions.Options;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Infrastructure.Settings;

namespace RouteOptimizer.Infrastructure.Services;

public sealed class ClientUrlBuilder : IClientUrlBuilder
{
    private readonly ClientAppSettings _settings;

    public ClientUrlBuilder(IOptions<ClientAppSettings> options) => _settings = options.Value;

    public string ResetPassword(string token) => Build(_settings.ResetPasswordPath, token);

    public string AcceptInvitation(string token) => Build(_settings.AcceptInvitationPath, token);

    private string Build(string path, string token)
    {
        var baseUri = new Uri(_settings.BaseUrl, UriKind.Absolute);
        var builder = new UriBuilder(new Uri(baseUri, path))
        {
            Query = $"token={Uri.EscapeDataString(token)}"
        };

        return builder.Uri.ToString();
    }
}
