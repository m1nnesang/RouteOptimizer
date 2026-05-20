using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RefreshTokenEntity = RouteOptimizer.Domain.Entities.RefreshToken;

namespace RouteOptimizer.Application.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordHasher _passwordHasher;
  private readonly ITokenService _tokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IUnitOfWork _unitOfWork;

  public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork)
  {
    _userRepository = userRepository;
    _passwordHasher = passwordHasher;
    _tokenService = tokenService;
    _refreshTokenRepository = refreshTokenRepository;
    _unitOfWork = unitOfWork;
  }

  public async Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken ct = default)
  {
    var user = await _userRepository.GetByEmailAsync(request.Email, ct);

    if (user is null || !user.IsActive)
      return Result<AuthTokenDto>.Failure("Invalid credentials");

    var hash = _passwordHasher.Verify(request.Password, user.PasswordHash);

    if (hash == false)
      return Result<AuthTokenDto>.Failure("Invalid credentials");

    var accessToken = _tokenService.GenerateAccessToken(user);
    var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();

   var refreshToken = RefreshTokenEntity.Create(user.Id, tokenHash, _tokenService.RefreshTokenExpiration);

   await _refreshTokenRepository.AddAsync(refreshToken, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<AuthTokenDto>.Success(new AuthTokenDto(accessToken, rawToken,refreshToken.ExpiresAt));
  }
}
