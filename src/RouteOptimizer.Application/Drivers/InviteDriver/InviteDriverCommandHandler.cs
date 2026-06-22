using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Drivers.InviteDriver;

public class InviteDriverCommandHandler : IRequestHandler<InviteDriverCommand,Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserInvitationRepository _invitationRepository;
    private readonly IMailService _mailService;
    private readonly IClientUrlBuilder _clientUrlBuilder;

    public InviteDriverCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork,
        IUserInvitationRepository userInvitationRepository, IMailService mailService,
        IClientUrlBuilder clientUrlBuilder)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _invitationRepository = userInvitationRepository;
        _mailService = mailService;
        _clientUrlBuilder = clientUrlBuilder;
    }

    public async Task<Result> Handle(InviteDriverCommand request, CancellationToken ct)
    {
        var email = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (email is not null)
            return Result.Failure("Email already in use");

        if (!Enum.TryParse<LicenseCategory>(request.LicenseCategory ,out var license))
            return Result.Failure("Invalid license category");

        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        if (phoneNumber.IsFailure)
            return Result.Failure(phoneNumber.Error ?? "Invalid phone number");

        var driverProfile = new DriverProfile(license);

        var user = User.Create(request.Email, request.FirstName, request.LastName, "PENDING", phoneNumber.Value!,
            UserRole.Driver, request.WarehouseId, driverProfile);

        if (user.IsFailure)
            return Result.Failure(user.Error ?? "Invalid user");

        await _userRepository.AddAsync(user.Value!, ct);


        var invitation = UserInvitation.Create(user.Value!.Id, TimeSpan.FromDays(7));
        await _invitationRepository.AddAsync(invitation, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        var message = new MailMessage(
            request.Email,
            "Invitation to Route Optimizer",
            $"Registration link: {_clientUrlBuilder.AcceptInvitation(invitation.Token)}"
        );

        var mailResult = await _mailService.SendAsync(message, ct);

        if (mailResult.IsFailure)
            return Result.Failure(mailResult.Error ?? "Failed to send invitation email");

        return Result.Success();
    }
}
