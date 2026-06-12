using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Users.DeactivateUser;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public DeactivateUserCommandHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);

        if (user is null)
            throw new NotFoundException("User is not found");

        if (!user.IsActive)
            return Result.Failure("User is already deactivated");

        user.Deactivate();

        return Result.Success();
    }
}
