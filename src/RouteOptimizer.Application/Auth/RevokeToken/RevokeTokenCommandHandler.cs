using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Auth.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, ITokenService tokenService) =>
    (_refreshTokenRepository, _tokenService) = (refreshTokenRepository, tokenService);

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var hash = _tokenService.HashToken(request.Token);
        var refreshToken = await _refreshTokenRepository.GetByHashAsync(hash, ct);

        if (refreshToken is null || !refreshToken.IsActive)
            return Result.Failure("Invalid or expired token");

        refreshToken.Revoke();

        return Result.Success();
    }
}
