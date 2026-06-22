namespace RouteOptimizer.Application.Abstractions;

public interface IClientUrlBuilder
{
    string ResetPassword(string token);

    string AcceptInvitation(string token);
}
