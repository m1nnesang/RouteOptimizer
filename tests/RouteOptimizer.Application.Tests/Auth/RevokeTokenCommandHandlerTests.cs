using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth.RevokeToken;
using RouteOptimizer.Domain.Entities;

namespace RouteOptimizer.Application.Tests.Auth;

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RevokeTokenCommandHandler _handler;

    public RevokeTokenCommandHandlerTests()
    {
        _handler = new RevokeTokenCommandHandler(
            _refreshTokenRepo.Object,
            _tokenService.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var command = new RevokeTokenCommand("nonexistent-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    [Fact]
    public async Task Handle_TokenIsRevoked_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));
        token.Revoke();

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);

        var command = new RevokeTokenCommand("already-revoked-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    [Fact]
    public async Task Handle_TokenIsExpired_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromSeconds(-1));

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);

        var command = new RevokeTokenCommand("expired-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidToken_RevokesTokenAndReturnsSuccess()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);

        var command = new RevokeTokenCommand("valid-token");

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    #endregion
}
