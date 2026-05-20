using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.AcceptInvitation;

public sealed record AcceptInvitationCommand(string InvitationToken, string Password) : ICommand<Result>;