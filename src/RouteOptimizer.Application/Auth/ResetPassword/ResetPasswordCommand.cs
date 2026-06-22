using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand<Result>;
