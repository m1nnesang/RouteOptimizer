using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Auth.AcceptInvitation;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserInvitationRepository _userInvitationRepository;
    private readonly IPasswordHasher _passwordHasher;


    public AcceptInvitationCommandHandler(IUserRepository userRepository, IUserInvitationRepository userInvitationRepository, IPasswordHasher passwordHasher) =>
        (_userRepository, _userInvitationRepository, _passwordHasher) = (userRepository, userInvitationRepository, passwordHasher);

    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        var invitation = await _userInvitationRepository.GetByTokenAsync(UserInvitation.Hash(request.InvitationToken), ct);

        if (invitation is null || invitation.IsUsed || invitation.IsExpired)
            return Result.Failure("Invalid or expired invitation");

        var user = await _userRepository.GetByIdAsync(invitation.UserId, ct);

        if (user is null)
            return Result.Failure("User not found");

        user.SetPassword(_passwordHasher.Hash(request.Password));
        invitation.Use();

        return Result.Success();
    }
}
