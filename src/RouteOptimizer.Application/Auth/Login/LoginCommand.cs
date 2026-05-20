using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<AuthTokenDto>>;
