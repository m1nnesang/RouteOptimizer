using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.AcceptInvitation;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserInvitationRepository _userInvitationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;


    public AcceptInvitationCommandHandler(IUserRepository userRepository, IUserInvitationRepository userInvitationRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork) =>
        (_userRepository, _userInvitationRepository, _passwordHasher, _unitOfWork) = (userRepository, userInvitationRepository, passwordHasher, unitOfWork);

    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        var invitation = await _userInvitationRepository.GetByTokenAsync(request.InvitationToken, ct);

        if (invitation is null || invitation.IsUsed || invitation.IsExpired)
            return Result.Failure("Invalid or expired invitation");

        var user = await _userRepository.GetByIdAsync(invitation.UserId, ct);

        if (user is null)
            return Result.Failure("User not found");

        user.SetPassword(_passwordHasher.Hash(request.Password));
        invitation.Use();

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
