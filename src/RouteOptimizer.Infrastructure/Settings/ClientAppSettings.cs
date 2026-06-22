namespace RouteOptimizer.Infrastructure.Settings;

public sealed class ClientAppSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5080";

    public string ResetPasswordPath { get; set; } = "/reset-password";

    public string AcceptInvitationPath { get; set; } = "/accept-invitation";
}
