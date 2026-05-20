using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string Token) : ICommand<Result<AuthTokenDto>>;
