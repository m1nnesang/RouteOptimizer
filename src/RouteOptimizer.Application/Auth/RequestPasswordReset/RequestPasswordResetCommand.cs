using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : ICommand<Result>;
