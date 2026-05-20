using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.RevokeToken;

public sealed record RevokeTokenCommand(string Token) : ICommand<Result>;