using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Users.DeactivateUser;

public record DeactivateUserCommand(Guid UserId) : ICommand<Result>;
