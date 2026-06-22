using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth.ResetPassword;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("new-hash");
        _refreshRepo.Setup(x => x.GetActiveByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new ResetPasswordCommandHandler(
            _tokenRepo.Object, _userRepo.Object, _refreshRepo.Object, _passwordHasher.Object);
    }

    private static User ActiveUser() =>
        User.Create("driver@test.com", "Jan", "Kowalski", "old-hash",
            PhoneNumber.Create("+48601234567").Value!, UserRole.Driver, Guid.NewGuid(),
            new DriverProfile(LicenseCategory.B)).Value!;

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        _tokenRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        var result = await _handler.Handle(new ResetPasswordCommand("token", "NewPassword1!"), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenUsed_ReturnsFailure()
    {
        var token = PasswordResetToken.Create(Guid.NewGuid(), TimeSpan.FromHours(1));
        token.Use();
        _tokenRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await _handler.Handle(new ResetPasswordCommand(token.Token, "NewPassword1!"), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenExpired_ReturnsFailure()
    {
        var token = PasswordResetToken.Create(Guid.NewGuid(), TimeSpan.FromSeconds(-1));
        _tokenRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await _handler.Handle(new ResetPasswordCommand(token.Token, "NewPassword1!"), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidToken_SetsPasswordUsesTokenAndRevokesRefreshTokens()
    {
        var user = ActiveUser();
        var token = PasswordResetToken.Create(user.Id, TimeSpan.FromHours(1));
        var refreshToken = RefreshToken.Create(user.Id, "refresh-hash", TimeSpan.FromHours(12));

        _tokenRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _refreshRepo.Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([refreshToken]);

        var result = await _handler.Handle(new ResetPasswordCommand(token.Token, "NewPassword1!"), default);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        token.IsUsed.Should().BeTrue();
        refreshToken.IsRevoked.Should().BeTrue();
    }
}
