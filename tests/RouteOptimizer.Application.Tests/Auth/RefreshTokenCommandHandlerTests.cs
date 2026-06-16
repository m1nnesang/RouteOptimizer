using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth.RefreshToken;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _tokenService.Object,
            _refreshTokenRepo.Object,
            _userRepo.Object,
            _unitOfWork.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var command = new RefreshTokenCommand("nonexistent-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    [Fact]
    public async Task Handle_TokenIsRevoked_ReturnsFailureAndRevokesChain()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));
        token.Revoke();

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);
        _refreshTokenRepo.Setup(x => x.GetActiveByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken>());

        var command = new RefreshTokenCommand("revoked-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
        _refreshTokenRepo.Verify(x => x.GetActiveByUserIdAsync(token.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenIsExpired_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromSeconds(-1));

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);

        var command = new RefreshTokenCommand("expired-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);
        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new RefreshTokenCommand("valid-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    [Fact]
    public async Task Handle_UserIsInactive_ReturnsFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var token = RefreshToken.Create(user.Id, "hash", TimeSpan.FromDays(7));

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);
        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)user);

        var command = new RefreshTokenCommand("valid-token");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired token");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAuthTokenDto()
    {
        var user = CreateActiveUser();
        var token = RefreshToken.Create(user.Id, "hash", TimeSpan.FromDays(7));

        _tokenService.Setup(x => x.HashToken(It.IsAny<string>())).Returns("some-hash");
        _refreshTokenRepo.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)token);
        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)user);
        _tokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("new-access-token");
        _tokenService.Setup(x => x.GenerateRefreshToken()).Returns(("new-raw-token", "new-hash"));
        _tokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(7));
        _refreshTokenRepo.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new RefreshTokenCommand("valid-token");

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-raw-token");
    }

    #endregion

    private User CreateActiveUser()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return User.Create("test@test.com", "Jan", "Kowalski", "hashedpassword", phone, UserRole.Dispatcher, Guid.NewGuid(),
            null).Value!;
    }
}
