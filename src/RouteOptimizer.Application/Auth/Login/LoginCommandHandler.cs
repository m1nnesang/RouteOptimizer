using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RefreshTokenEntity = RouteOptimizer.Domain.Entities.RefreshToken;

namespace RouteOptimizer.Application.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private const string DummyHash = "AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var passwordValid = _passwordHasher.Verify(request.Password, hashToVerify);

        if (user is null || !user.IsActive || !passwordValid)
            return Result<AuthTokenDto>.Failure("Invalid credentials");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();

        var refreshToken = RefreshTokenEntity.Create(user.Id, tokenHash, _tokenService.RefreshTokenExpiration);

        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        return Result<AuthTokenDto>.Success(new AuthTokenDto(accessToken, rawToken, refreshToken.ExpiresAt));
    }
}
