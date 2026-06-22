using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Auth.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMailService _mailService;

    public RequestPasswordResetCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        IMailService mailService) =>
        (_userRepository, _tokenRepository, _unitOfWork, _mailService) =
        (userRepository, tokenRepository, unitOfWork, mailService);

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (user is null || !user.IsActive)
            return Result.Success();

        var resetToken = PasswordResetToken.Create(user.Id, TokenLifetime);
        await _tokenRepository.AddAsync(resetToken, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var message = new MailMessage(
            user.Email,
            "Password reset for Route Optimizer",
            $"Reset link: https://app/reset-password?token={resetToken.Token}"
        );

        await _mailService.SendAsync(message, ct);

        return Result.Success();
    }
}
