using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Auth.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher) =>
        (_tokenRepository, _userRepository, _refreshTokenRepository, _passwordHasher) =
        (tokenRepository, userRepository, refreshTokenRepository, passwordHasher);

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var resetToken = await _tokenRepository.GetByTokenAsync(PasswordResetToken.Hash(request.Token), ct);

        if (resetToken is null || resetToken.IsUsed || resetToken.IsExpired)
            return Result.Failure("Invalid or expired password reset token");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, ct);

        if (user is null || !user.IsActive)
            return Result.Failure("User not found");

        user.SetPassword(_passwordHasher.Hash(request.NewPassword));
        resetToken.Use();

        var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(user.Id, ct);
        foreach (var token in activeTokens)
            token.Revoke();

        return Result.Success();
    }
}
