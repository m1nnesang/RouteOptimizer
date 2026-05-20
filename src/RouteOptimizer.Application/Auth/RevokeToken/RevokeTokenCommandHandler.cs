using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, ITokenService tokenService, IUnitOfWork unitOfWork) =>
    (_refreshTokenRepository, _tokenService, _unitOfWork) = (refreshTokenRepository, tokenService, unitOfWork);

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var hash = _tokenService.HashToken(request.Token);
        var refreshToken = await _refreshTokenRepository.GetByHashAsync(hash, ct);

        if (refreshToken is null || !refreshToken.IsActive)
            return Result.Failure("Invalid or expired token");

        refreshToken.Revoke();
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
