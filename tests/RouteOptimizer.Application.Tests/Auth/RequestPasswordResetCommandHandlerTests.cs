using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth.RequestPasswordReset;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Auth;

public class RequestPasswordResetCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMailService> _mailService = new();
    private readonly RequestPasswordResetCommandHandler _handler;

    public RequestPasswordResetCommandHandlerTests()
    {
        _mailService.Setup(x => x.SendAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result.Success());

        _handler = new RequestPasswordResetCommandHandler(
            _userRepo.Object, _tokenRepo.Object, _unitOfWork.Object, _mailService.Object);
    }

    private static User ActiveUser() =>
        User.Create("driver@test.com", "Jan", "Kowalski", "hash",
            PhoneNumber.Create("+48601234567").Value!, UserRole.Driver, Guid.NewGuid(),
            new DriverProfile(LicenseCategory.B)).Value!;

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsSuccessWithoutCreatingToken()
    {
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new RequestPasswordResetCommand("unknown@test.com"), default);

        result.IsSuccess.Should().BeTrue();
        _tokenRepo.Verify(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _mailService.Verify(x => x.SendAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsSuccessWithoutCreatingToken()
    {
        var user = ActiveUser();
        user.Deactivate();
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new RequestPasswordResetCommand("driver@test.com"), default);

        result.IsSuccess.Should().BeTrue();
        _tokenRepo.Verify(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KnownActiveUser_CreatesTokenSavesAndSendsEmail()
    {
        var user = ActiveUser();
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new RequestPasswordResetCommand("driver@test.com"), default);

        result.IsSuccess.Should().BeTrue();
        _tokenRepo.Verify(x => x.AddAsync(It.Is<PasswordResetToken>(t => t.UserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mailService.Verify(x => x.SendAsync(It.Is<MailMessage>(m => m.To == user.Email), It.IsAny<CancellationToken>()), Times.Once);
    }
}
