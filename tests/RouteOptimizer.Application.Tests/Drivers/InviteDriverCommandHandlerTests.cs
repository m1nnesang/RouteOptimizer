using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Drivers.InviteDriver;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Drivers;

public class InviteDriverCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUserInvitationRepository> _invitationRepo = new();
    private readonly Mock<IMailService> _mailService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClientUrlBuilder> _clientUrlBuilder = new();
    private readonly InviteDriverCommandHandler _handler;

    public InviteDriverCommandHandlerTests()
    {
        _handler = new InviteDriverCommandHandler(
            _userRepo.Object,
            _unitOfWork.Object,
            _invitationRepo.Object,
            _mailService.Object,
            _clientUrlBuilder.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_EmailAlreadyInUse_ReturnsFailure()
    {
        var existingUser = CreateDriver();
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email already in use");
    }

    [Fact]
    public async Task Handle_InvalidLicenseCategory_ReturnsFailure()
    {
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = ValidCommand() with { LicenseCategory = "INVALID" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid license category");
    }

    [Fact]
    public async Task Handle_InvalidPhoneNumber_ReturnsFailure()
    {
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = ValidCommand() with { PhoneNumber = "not-a-phone" };

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MailServiceFails_ReturnsFailure()
    {
        SetupEmailNotInUse();
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mailService.Setup(x => x.SendAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP error"));

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SMTP error");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_SavesUserCreatesInvitationAndSendsEmail()
    {
        SetupEmailNotInUse();
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mailService.Setup(x => x.SendAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _handler.Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _invitationRepo.Verify(x => x.AddAsync(It.IsAny<UserInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mailService.Verify(x => x.SendAsync(It.IsAny<MailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    private void SetupEmailNotInUse() =>
        _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

    private static InviteDriverCommand ValidCommand() =>
        new("driver@test.com", "Jan", "Kowalski", "+48601234567", Guid.NewGuid(), nameof(LicenseCategory.B));

    private static User CreateDriver()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return User.Create("driver@test.com", "Jan", "Kowalski", "hashed", phone, UserRole.Driver, Guid.NewGuid(),
            new DriverProfile(LicenseCategory.B)).Value!;
    }
}
