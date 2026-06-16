using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RefreshTokenEntity = RouteOptimizer.Domain.Entities.RefreshToken;

namespace RouteOptimizer.Application.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository, IUnitOfWork unitOfWork) =>
        (_tokenService, _refreshTokenRepository, _userRepository, _unitOfWork) = (tokenService, refreshTokenRepository, userRepository, unitOfWork);

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = _tokenService.HashToken(request.Token);
        var refreshToken = await _refreshTokenRepository.GetByHashAsync(hash, ct);

        if (refreshToken is null)
            return Result<AuthTokenDto>.Failure("Invalid or expired token");

        if (refreshToken.IsRevoked)
        {
            var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(refreshToken.UserId, ct);
            foreach (var t in activeTokens)
                t.Revoke();
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<AuthTokenDto>.Failure("Invalid or expired token");
        }

        if (!refreshToken.IsActive)
            return Result<AuthTokenDto>.Failure("Invalid or expired token");

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, ct);
        if (user is null || !user.IsActive)
            return Result<AuthTokenDto>.Failure("Invalid or expired token");

        refreshToken.Revoke();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();

        var newToken = RefreshTokenEntity.Create(user.Id, tokenHash, _tokenService.RefreshTokenExpiration);

        await _refreshTokenRepository.AddAsync(newToken, ct);

        return Result<AuthTokenDto>.Success(new AuthTokenDto(accessToken, rawToken, newToken.ExpiresAt));
    }
}
