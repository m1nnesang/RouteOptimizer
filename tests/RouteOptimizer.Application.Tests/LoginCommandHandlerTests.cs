using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth.Login;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepo.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _refreshTokenRepo.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _userRepo.Setup(x => x.GetByEmailAsync("test@gmail.com", default)).ReturnsAsync((User?)null);

        var command = new LoginCommand("test@gmail.com", "password");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Handle_UserIsInactive_ReturnsFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();

        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)user);

        var command = new LoginCommand("test@test.com", "password");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var user = CreateActiveUser();

        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)user);
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var command = new LoginCommand("test@test.com", "wrongpassword");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid credentials");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthTokenDto()
    {
        var user = CreateActiveUser();

        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)user);
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _tokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        _tokenService.Setup(x => x.GenerateRefreshToken())
            .Returns(("raw-token", "hashed-token"));
        _tokenService.Setup(x => x.RefreshTokenExpiration)
            .Returns(TimeSpan.FromDays(7));
        _refreshTokenRepo.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new LoginCommand("test@test.com", "correctpassword");

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("raw-token");
    }

    #endregion

    private User CreateActiveUser()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return User.Create("test@test.com", "Jan", "Kowalski", "hashedpassword", phone, UserRole.Dispatcher, Guid.NewGuid(),
            null).Value!;
    }
}
